using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline;

namespace Quacklibs.AzureDevopsCli.Commands.WorkItems
{
    internal class WorkItemCreateCommand : BaseCommand
    {
        private readonly AzureDevopsService _service;
        private readonly WorkItemReadCommand _workItemReadCommand;

        private Option<WorkItemKind> _workItemKindOption = new("--type")
        {
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = (_) => WorkItemKind.Task
        };

        public WorkItemCreateCommand(AzureDevopsService service, WorkItemReadCommand workItemReadCommand) : base(CommandConstants.CreateCommand, "Create a new work item")
        {
            _service = service;
            _workItemReadCommand = workItemReadCommand;

            var complationItems = CompletiontionItems.FromEnum<WorkItemKind>().ToArray();
            _workItemKindOption.CompletionSources.Add(ctx => complationItems);

            this.Options.Add(_workItemKindOption);
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var workItemType = parseResult.GetValue(_workItemKindOption);
            var assignedTo = base.Settings.UserEmail;
            var witClient = _service.GetClient<WorkItemTrackingHttpClient>();

            //show the active & new workitems so the task can be put under the correct parent
            await _workItemReadCommand.ReadAndDisplayWorkItems(assignedTo: null, states: [WorkItemState.Active, WorkItemState.New]);

            AnsiConsole.WriteLine($"\n Creating a new work item of type {workItemType}", new Style(foreground: Color.Green, decoration: Decoration.Bold));

            Console.WriteLine("\n Title of the work item");
            var title = Console.ReadLine();
            Console.WriteLine("\n Description");
            var description = Console.ReadLine();
            Console.WriteLine("\n parentId - pick a parent from the list under which the task will be created");
            var parentId = int.Parse(Console.ReadLine() ?? "0");

            var patchDocument = new JsonPatchDocument
            {
                // Set title
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = title
                },
                // Description
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = description
                },
                // Link to parent
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        //create a link from the task to the parent
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = $"{base.Settings.OrganizationUrl}/_apis/wit/workItems/{parentId}",
                        attributes = new { comment = "Linked as child task" }
                    }
                },
            };
            if (!string.IsNullOrEmpty(assignedTo))
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.AssignedTo",
                    Value = assignedTo
                });
            }

            // Get the parent work item. Adding this will ensure that the task get's shown on the current board
            var parentWorkItem = await witClient.GetWorkItemAsync(parentId, [AzureDevopsFields.IterationPath, AzureDevopsFields.TeamProject]);
            var iterationPath = parentWorkItem.Fields["System.IterationPath"]?.ToString();
            var teamProject = parentWorkItem.Fields[AzureDevopsFields.TeamProject]?.ToString();

            //add the iteration path from the parent
            if (!string.IsNullOrEmpty(iterationPath))
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.IterationPath",
                    Value = iterationPath
                });
            }

            // Create the task
            var createdWorkItem = await witClient.CreateWorkItemAsync(patchDocument, teamProject, workItemType.ToString());

            if (createdWorkItem == null)
            {
                Console.WriteLine("Failed to create workitem");
                return ExitCodes.Ok;
            }

            var uri = new WorkItemLinkType(base.Settings.OrganizationUrl, createdWorkItem.Id!.Value).ToWorkItemUrl();

            AnsiConsole.WriteLine($"\n created {createdWorkItem.Id}. Type: {workItemType.ToString()}");
            AnsiConsole.Write(uri, new Style(foreground: Color.Blue));
            AnsiConsole.WriteLine($"run {WorkItemOpenCommand.CommandHelpTextWithParameter(createdWorkItem.Id.Value)} to open");

            return ExitCodes.Ok;
        }
    }
}