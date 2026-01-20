namespace Quacklibs.AzureDevopsCli.Core.Types
{
    public record AzureDevopsUserType(string Id, string Email, string DisplayName);

    public record NoAzureDevopsUserFound(string Input) : AzureDevopsUserType(string.Empty, string.Empty, string.Empty);
}