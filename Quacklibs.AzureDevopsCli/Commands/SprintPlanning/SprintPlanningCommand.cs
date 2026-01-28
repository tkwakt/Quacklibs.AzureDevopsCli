namespace Quacklibs.AzureDevopsCli.Commands.SprintPlanning
{
    internal class SprintPlanningCommand : BaseCommand
    {
        public SprintPlanningCommand(SprintPlanningUpdateCommand updateCommand) : base("sprintplanning", "Tools to help with sprint planning")
        {
            this.Subcommands.Add(updateCommand);
        }
    }
}