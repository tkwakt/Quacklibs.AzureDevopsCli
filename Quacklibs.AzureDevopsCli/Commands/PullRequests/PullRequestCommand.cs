namespace Quacklibs.AzureDevopsCli.Commands.PullRequests
{
    public class PullRequestCommand : BaseCommand
    {
        public PullRequestCommand(PullRequestOpenCommand openCommand, PullRequestReadCommand readCommand) : base("pullrequest", "Open an pull request by id", "pr")
        {
            this.Subcommands.Add(openCommand);
            this.Subcommands.Add(readCommand);
        }
    }
}