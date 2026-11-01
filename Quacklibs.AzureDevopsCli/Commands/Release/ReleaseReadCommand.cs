using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.Work.WebApi;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline;
using Quacklibs.AzureDevopsCli.Core.Types;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli.Commands.Release;

/// <summary>
/// required permissions: Environment &amp; build
/// </summary>
internal class ReleaseReadCommand : BaseCommand
{
    private readonly AzureDevopsService _service;
    private readonly AzureDevopsProjectService _projectService;

    private readonly Option<string> _projectOption = new(CommandOptionConstants.ProjectOptionName)
    {
        Required = false,
        Description = "the project to run the command on"
    };

    private readonly Option<string> _sinceOption = new(CommandOptionConstants.SinceOptionName, CommandOptionConstants.SinceOptionAliasses)
    {
        Required = false,
        Description = "The maximum age of releases to include.",
        Arity = ArgumentArity.ExactlyOne,
        DefaultValueFactory = _ => "6m"
    };

    public ReleaseReadCommand(AzureDevopsService service, AzureDevopsProjectService projectService) : base(CommandConstants.ReadCommand, "read all changes for an (upcomming) releasae", "-r")
    {
        _service = service;
        _projectService = projectService;

        _projectOption.DefaultValueFactory = (_) => Settings.DefaultProject;

        Options.Add(_projectOption);
        Options.Add(_sinceOption);

        _sinceOption.CompletionSources.Add(_ => SinceParser.ToCompletionOptions());
    }

    protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
    {
        var builds = _service.GetClient<BuildHttpClient>();
        var gitClient = _service.GetClient<GitHttpClient>();

        var projectOption = parseResult.GetValue(_projectOption);
        var sinceInput = new SinceType(parseResult.GetValue(_sinceOption)!);
        var releaseTimeRange = sinceInput.ToDateTimeRange();

        var selectedProject = await _projectService.GetOrSelectProjectAsync(projectOption);

        if (selectedProject is NoTeamProjectFoundResult)
        {
            Console.WriteLine($"No project found for {projectOption}");
            return ExitCodes.Error;
        }

        var repo = await gitClient.GetRepositoriesAsync(selectedProject.Id);

        var repoPrompt = new SelectionPrompt<GitRepository>()
            .Title("Select [green]repo[/]:")
            .PageSize(10)
            .AddChoices(repo.ToArray())
            .UseConverter(e => e.Name);

        var selectedRepo = AnsiConsole.Prompt(repoPrompt);

        var refs = await gitClient.GetRefsAsync(project: selectedProject.FullProjectName, selectedRepo.Name);

        if (!refs.Any())
        {
            Console.WriteLine("No branches found in the repository.");
            return ExitCodes.Error;
        }

        var fromPrompt = new SelectionPrompt<GitRef>()
            .Title("Select [green]From[/]:")
            .PageSize(10)
            .AddChoices(refs.ToArray())
            .UseConverter(e => e.Name);

        var toPrompt = new SelectionPrompt<GitRef>()
            .Title("Select [blue]To[/]:")
            .PageSize(10)
            .AddChoices(refs.ToArray())
            .UseConverter(e => e.Name);

        var fromBranch = AnsiConsole.Prompt(fromPrompt);
        var toBranch = AnsiConsole.Prompt(toPrompt);

        // Query commits between branches
        var criteria = new GitQueryCommitsCriteria
        {
            ItemVersion = new GitVersionDescriptor { Version = fromBranch.Name, VersionType = GitVersionType.Branch },
            CompareVersion = new GitVersionDescriptor { Version = toBranch.Name, VersionType = GitVersionType.Branch }
        };

        var commits = await gitClient.GetCommitsAsync(selectedProject.FullProjectName, selectedProject.Id, criteria);

        foreach (var commit in commits)
        {
            AnsiConsole.WriteLine($"{commit.Comment} {commit.CommitId} {commit.Links} {commit.Committer.Name}");
        }

        return ExitCodes.Ok;
    }
}
