using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Quacklibs.AzureDevopsCli.Commands.PullRequests
{
    public class PullRequestOpenCommand : BaseCommand
    {
        private readonly AzureDevopsService _service;

        public Option<int> PullRequestIdOption = new("--id");
        public Argument<int> PullRequestIdArgument = new("The ID of the pull request to open");


        public PullRequestOpenCommand(AzureDevopsService service) : base("open", "Open a pull request in the browser")
        {
            base.Options.Add(PullRequestIdOption);
            base.Arguments.Add(PullRequestIdArgument);

            this._service = service;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var gitClient = _service.GetClient<GitHttpClient>();

            int pullRequestIdOption = parseResult.GetValue(PullRequestIdOption);
            int pullRequestIdArgument = parseResult.GetValue(PullRequestIdArgument);

            var pullRequestId = pullRequestIdOption != 0 ? pullRequestIdOption : pullRequestIdArgument;

            var pr = await gitClient.GetPullRequestByIdAsync(pullRequestId);

            if (pr == null)
            {
                AnsiConsole.WriteLine($"No pull request found for id {pullRequestId}");
                return ExitCodes.Ok;
            }

            // Build the UI link using the repository URL + "/pullrequest/" + PR id
            string prWebUrl = $"{pr.Repository.RemoteUrl}/pullrequest/{pullRequestId}";

            Console.WriteLine($"Opening {prWebUrl}");
            Browser.Open(prWebUrl);

            return ExitCodes.Ok;

        }
    }
}