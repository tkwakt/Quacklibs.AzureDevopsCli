namespace Quacklibs.AzureDevopsCli.Commands.WorkItems
{
    public class WorkItemOpenCommand : BaseCommand
    {
        private static string BaseCommandHelpText => $"{CommandConstants.BaseCommand} workitem open";
        public static string CommandHelpText => $"{BaseCommandHelpText} (id)";
        public static string CommandHelpTextWithParameter(int workItemId) => $"{BaseCommandHelpText} {workItemId}";

        private Option<int> WorkItemIdOption = new("--id");
        private Argument<int> WorkItemIdArgument = new("The ID of the work item to open");

        public WorkItemOpenCommand() : base("open", "Open a work item in the browser")
        {
            this.Options.Add(WorkItemIdOption);
            this.Arguments.Add(WorkItemIdArgument);
        }

        protected override Task<int> OnExecuteAsync(ParseResult context)
        {
            int workItemIdOption = context.GetValue(WorkItemIdOption);
            int workItemIdArgument = context.GetValue(WorkItemIdArgument);

            var workItemId = workItemIdOption != 0 ? workItemIdOption : workItemIdArgument;

            string uri = $"{base.Settings.OrganizationUrl}/_workitems/edit/{workItemId}";

            Console.WriteLine($"Opening {uri}");
            Browser.Open(uri);
            
            return Task.FromResult(ExitCodes.Ok);
        }
    }
}