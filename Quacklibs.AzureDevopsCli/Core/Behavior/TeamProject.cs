using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{

    public record TeamProjectResult(Guid Id, string FullProjectName);
    internal static class TeamProject
    {
        public static TeamProjectResult? TryGetTeamProject(this IPagedList<TeamProjectReference> projects, string searchQuery)
        {
            var result = projects.FirstOrDefault(e => e.Name.StartsWith(searchQuery, StringComparison.InvariantCultureIgnoreCase));

            if (result == null)
                result = projects.FirstOrDefault(e => e.Name.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase));

            return result == null ? null : new TeamProjectResult(result.Id, result.Name);
        }
    }
}