namespace Quacklibs.AzureDevopsCli.Commands.WorkItems
{
    internal class WorkItemCommand : BaseCommand
    {
        public WorkItemCommand(WorkItemCreateCommand createCommand, WorkItemReadCommand readCommand, WorkItemOpenCommand openCommand) : base("workitem", "Create read or open workitems", aliasses: "wi")
        {
            this.Subcommands.Add(createCommand);
            this.Subcommands.Add(readCommand);
            this.Subcommands.Add(openCommand);
        }
    }
}