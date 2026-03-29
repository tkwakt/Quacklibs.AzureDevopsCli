using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Quacklibs.AzureDevopsCli.Services;


namespace Quacklibs.AzureDevopsCli.Commands.SprintPlanning
{
    internal class SprintPlanningUpdateCommand : BaseCommand
    {
        private readonly Option<string> _forOption = new(CommandOptionConstants.ForOptionName)
        {
            Required = false,
            Description = "Filter the report by person. The value can be an email address or (part of) a name. \r\n" +
                          "If multiple users match, an interactive selection is shown"
        };
        private readonly Option<string> _projectOption = new(CommandOptionConstants.ProjectOptionName)
        {
            Required = false,
            Description = "the project to run the command on"
        };

        private readonly AzureDevopsUserService _users;
        private readonly AzureDevopsProjectService _projectService;


        public AzureDevopsService _service { get; }


        public SprintPlanningUpdateCommand(AzureDevopsService service, AzureDevopsUserService users, AzureDevopsProjectService projectService, SettingsService options) : base(CommandConstants.UpdateCommand, "Move workitems from one sprint to another")
        {
            _service = service;
            _users = users;
            _projectService = projectService;

            _projectOption.DefaultValueFactory = _ => Settings.DefaultProject;

            this.Add(_projectOption);
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var forOptionResult = parseResult.GetValue(_forOption);
            var projectOptionResult = parseResult.GetValue(_projectOption);

            //var projects = await _service.GetClient<ProjectHttpClient>().GetProjects();
            var targetUser = await _users.GetOrSelectUserAsync(forOptionResult);
            var userSearchQuery = targetUser.DisplayName;

            if (targetUser is NoAzureDevopsUserFound)
            {
                userSearchQuery = "@all";
            }
            
            var selectedProject = await _projectService.GetOrSelectProjectAsync(projectOptionResult);

            if(selectedProject is NoTeamProjectFoundResult)
            {
                Console.WriteLine($"No project found for {projectOptionResult}");
                return ExitCodes.Error;
            }

            Console.WriteLine($"Querying for {userSearchQuery}, project {selectedProject.FullProjectName} ");

            var client = _service.GetClient<WorkHttpClient>();
            var teamContext = new TeamContext(selectedProject.Id);

            var iterations = await client.GetTeamIterationsAsync(teamContext);
            iterations = iterations.Where(e => e.Attributes.StartDate > DateTime.Now.AddDays(-50)).ToList();

            var pastIterations = iterations.Where(e => e.Attributes.TimeFrame == TimeFrame.Past || e.Attributes.TimeFrame == TimeFrame.Current);
            var futureIterations = iterations.Where(e => e.Attributes.TimeFrame == TimeFrame.Current || e.Attributes.TimeFrame == TimeFrame.Future);

            Func<TeamSettingsIteration, string> displayString = e => $"{e.Name}. {e.Path}, {e.Attributes.StartDate.Value.ToString(Defaults.DateFormat)} - {e.Attributes.FinishDate.Value.ToString(Defaults.DateFormat)}";

            var fromPrompt = new SelectionPrompt<TeamSettingsIteration>()
           .Title("Select [green]From[/]:")
           .PageSize(10)
           .AddChoices(pastIterations.ToArray())
           .UseConverter(displayString);

            var toPrompt = new SelectionPrompt<TeamSettingsIteration>()
                .Title("Select [blue]To[/]:")
                .PageSize(10)
                .AddChoices(futureIterations.ToArray())
                .UseConverter(displayString);


            var fromIteration = AnsiConsole.Prompt(fromPrompt);
            var toIteration = AnsiConsole.Prompt(toPrompt);

            var assignedToWiql = new AssignedUserWiqlQueryPart(base.Settings.UserEmail).Get(userSearchQuery);

            var wiql = new Wiql()
            {
                Query = $@"
        SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType]
        FROM WorkItems
        WHERE [System.IterationPath] = '{fromIteration.Path}'
        AND [System.State] IN ('New', 'Active')
        {assignedToWiql}
        ORDER BY [System.Id]"
            };

            var workItemClient = _service.GetClient<WorkItemTrackingHttpClient>();

            var workItemsInCurrentIteration = await workItemClient.QueryByWiqlAsync(wiql, selectedProject.Id);

            var workitemIdsInCurrentIteration = workItemsInCurrentIteration.WorkItems.Select(e => e.Id);

            if (!workitemIdsInCurrentIteration.Any())
            {
                Console.WriteLine("No workitems found");
                return ExitCodes.Ok;
            }

            string[] fields = new[] { AzureDevopsFields.WorkItemState, AzureDevopsFields.WorkItemType, AzureDevopsFields.WorkItemAssignedTo, AzureDevopsFields.WorkItemTitle };
            var workItemsToMove = await workItemClient.GetWorkItemsAsync(workitemIdsInCurrentIteration, fields);

            var table = TableBuilder<WorkItem>
                .Create()
                .WithTitle("Workitems that will be moved")
                .WithColumn("Id", new ColumnValue<WorkItem>(e => e.Id.ToString()))
                .WithColumn("Title", new(e => e.Fields[AzureDevopsFields.WorkItemTitle].ToString()))
                .WithColumn("Type", new(e => e.Fields[AzureDevopsFields.WorkItemType].ToString()))
                .WithColumn("AssignedTo", new(e => e.Fields.TryGetFieldValue(AzureDevopsFields.WorkItemAssignedTo, "n/a").ToString()))
                .WithColumn("State", new(e => e.Fields[AzureDevopsFields.WorkItemState].ToString()))
                .WithColumn("url", new(e => e.Url.AsUrlMarkup("link")))
                .WithRows(workItemsToMove)
                .Build();

            AnsiConsole.Write(table);

            bool isConfirmed = AnsiConsole.Confirm($"Move workitems from {fromIteration.Name} ({fromIteration.Attributes.TimeFrame}) " +
                                                   $"to iteration {toIteration.Name} ({toIteration.Attributes.TimeFrame})");

            if (!isConfirmed)
                return ExitCodes.Ok;

            var jsonPatchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.IterationPath",
                    Value = toIteration.Path
                }
            };

            foreach (var workItemId in workItemsToMove.Select(e => e.Id).Where(e => e != null))
            {
                await workItemClient.UpdateWorkItemAsync(jsonPatchDocument, workItemId!.Value);
            }

            return ExitCodes.Ok;
        }
    }
}