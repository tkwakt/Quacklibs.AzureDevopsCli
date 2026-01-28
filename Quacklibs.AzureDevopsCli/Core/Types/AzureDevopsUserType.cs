namespace Quacklibs.AzureDevopsCli.Core.Types
{
    public record AzureDevopsUserType(Guid Id, string Email, string DisplayName);

    public record NoAzureDevopsUserFound(string Input) : AzureDevopsUserType(Guid.Empty, string.Empty, string.Empty);
}