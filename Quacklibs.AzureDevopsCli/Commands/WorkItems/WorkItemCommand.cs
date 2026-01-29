namespace Quacklibs.AzureDevopsCli.Commands.WorkItems
{
    internal class WorkItemCommand : BaseCommand
    {
        public WorkItemCommand(WorkItemCreateCommand createCommand, WorkItemReadCommand readCommand, WorkItemOpenCommand openCommand, WorkItemUpdateCommand updateCommand) : base("workitem", "Create read or open workitems", aliasses: "wi")
        {
            Subcommands.Add(createCommand);
            Subcommands.Add(readCommand);
            Subcommands.Add(openCommand);
            Subcommands.Add(updateCommand);
        }
    }
}