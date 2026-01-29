namespace Quacklibs.AzureDevopsCli.Commands.Project
{
    public class ProjectCommand : BaseCommand
    {
        public ProjectCommand(ProjectReadCommand projectReadCommand) : base("project", "changes on projects")
        {
            this.Subcommands.Add(projectReadCommand);
        }
    }
}