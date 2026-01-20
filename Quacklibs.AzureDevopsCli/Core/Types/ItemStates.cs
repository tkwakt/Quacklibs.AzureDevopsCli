namespace Quacklibs.AzureDevopsCli.Core.Types
{
    public enum WorkItemKind
    {
        Task,
        UserStory,
        Feature
    }

    public enum WorkItemState
    {
        New,
        Active,
        Closed,
        Resolved,
        Removed
    }
}