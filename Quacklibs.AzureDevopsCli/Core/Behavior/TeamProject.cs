using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{

    public record TeamProjectResult(Guid Id, string FullProjectName);
    public record NoTeamProjectFoundResult() : TeamProjectResult(Guid.Empty, "N/A");

}
