using Quacklibs.AzureDevopsCli.Core;
using Quacklibs.AzureDevopsCli.Core.Behavior;

namespace Quacklibs.AzureDevopsCli.Commands.WorkItems
{
    public class WorkItemOpenCommand : BaseCommand
    {
        private static string BaseCommandHelpText => $"{CommandConstants.BaseCommand} workitem open";
        public static string CommandHelpText => $"{BaseCommandHelpText} (id)";
        public static string CommandHelpTextWithParameter(int workItemId) => $"{BaseCommandHelpText} {workItemId}";

        public string Project { get; set; }
        public int Id { get; set; }

        public Option<int> WorkItemIdOption = new("--id");
        public Argument<int> WorkItemIdArgument = new("The ID of the pull request to open");


        public WorkItemOpenCommand() : base("open", "Open a work item in the browser")
        {
            Project = base.Settings.DefaultProject;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult context)
        {
            int pullRequestIdOption = context.GetValue(WorkItemIdOption);
            int pullRequestIdArgument = context.GetValue(WorkItemIdArgument);

            var pullRequestId = pullRequestIdOption != 0 ? pullRequestIdOption : pullRequestIdArgument;

            string uri = $"{base.Settings.OrganizationUrl}/_workitems/edit/{Id}";

            Console.WriteLine($"Opening {uri}");
            Browser.Open(uri);


            return ExitCodes.Ok;
        }
    }
}