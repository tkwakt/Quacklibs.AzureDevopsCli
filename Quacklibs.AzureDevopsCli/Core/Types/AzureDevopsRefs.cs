
namespace Quacklibs.AzureDevopsCli.Core.Types
{
    internal class AzureDevopsFields
    {
        public const string WorkItemType = "System.WorkItemType";
        public const string WorkItemId = "System.Id";
        public const string WorkItemTitle = "System.Title";
        public const string WorkItemState = "System.State";
        public const string WorkItemAssignedTo = "System.AssignedTo";
        public const string TeamProject = "System.TeamProject";

        public const string IterationPath = "System.IterationPath";

    }


    internal class AzureDevopsRefs
    {
        public const string ParentWorkItem = "System.LinkTypes.Hierarchy-Reverse";
    }

    internal class AzureDevopsWorkItemTypes
    {
        public const string WorkItem = "";
        public const string Feature = "Feature";
        public const string Epic = "Epic";
    }

}