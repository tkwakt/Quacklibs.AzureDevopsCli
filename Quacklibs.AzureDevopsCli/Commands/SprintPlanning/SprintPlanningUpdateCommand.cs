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
        public AzureDevopsService _service { get; }

        public string Project { get; set; }

        public string AssignedTo { get; set; } = "@all";

        public SprintPlanningUpdateCommand(AzureDevopsService service, SettingsService options) : base(CommandConstants.UpdateCommand, "Move workitems from one sprint to another")
        {
            _service = service;

            Project = base.Settings.DefaultProject;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var projects = await _service.GetClient<ProjectHttpClient>().GetProjects();

            var selectedProject = projects.TryGetTeamProject(Project);

            if (selectedProject == null)
            {
                Console.WriteLine("no matching project found");
                return ExitCodes.Ok;
            }

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

            var assignedToWiql = new AssignedUserWiqlQueryPart(base.Settings.UserEmail).Get(this.AssignedTo);

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