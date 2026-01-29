using Microsoft.TeamFoundation.Core.WebApi;


namespace Quacklibs.AzureDevopsCli.Commands.Project
{
    public class ProjectReadCommand : BaseCommand
    {
        private readonly AzureDevopsService _azdevopsService;

        public ProjectReadCommand(AzureDevopsService azdevopsService) : base("read", "Read all available projects")
        {
            _azdevopsService = azdevopsService;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var projects = await _azdevopsService.GetClient<ProjectHttpClient>()
                                                 .GetProjects(stateFilter: ProjectState.WellFormed);

            var projectsTable = TableBuilder<TeamProjectReference>
                                .Create()
                                .WithTitle("Projects")
                                .WithColumn("url", new(e => e.Url))
                                .WithColumn("name", new(e => e.Name))
                                .WithRows(projects.ToList() ?? [])
                                .Build();

            AnsiConsole.Write(projectsTable);

            return ExitCodes.Ok;
        }
    }
}