using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using System;

namespace Quacklibs.AzureDevopsCli.Services;

public class AzureDevopsProjectService
{
    public AzureDevopsService _azdoService { get; }

    public AzureDevopsProjectService(AzureDevopsService azdoService)
    {
        _azdoService = azdoService;
    }

    public async Task<TeamProjectResult> GetOrSelectProjectAsync(string? searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
        {
            return new NoTeamProjectFoundResult();
        }

        var projects = await _azdoService.GetClient<ProjectHttpClient>().GetProjects(stateFilter: ProjectState.WellFormed);
 
        var foundProjects = projects.Where(e => e.Name != null && e.Name.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                                           e?.Description != null && e.Description.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase));

        if (!foundProjects.Any())
        {
            AnsiConsole.WriteLine($"No matching projects found for {searchQuery}");
            return new NoTeamProjectFoundResult();
        }
        if (foundProjects.Count() == 1)
        {
            var selectedProject = foundProjects.First();
            return ToTeamProjectResult(selectedProject);
        }
        else
        {
            Func<TeamProjectReference, string> displayString = e => $"{e.Name}, {e.Description}";

            var userPrompt = new SelectionPrompt<TeamProjectReference>()
                .Title("Multipe possible projects found. Select one")
                .PageSize(100)
                .AddChoices(foundProjects.ToArray())
                .UseConverter(displayString);

            var projectChoice = AnsiConsole.Prompt(userPrompt);

            if (projectChoice == null)
                return new NoTeamProjectFoundResult();

            return ToTeamProjectResult(projectChoice);
        }
    }

    private TeamProjectResult ToTeamProjectResult(TeamProjectReference reference)
    {
        return new TeamProjectResult(reference.Id, reference.Name);
    }
}
