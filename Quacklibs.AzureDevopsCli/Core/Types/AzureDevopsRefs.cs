
namespace Quacklibs.AzureDevopsCli.Core.Types
{
    internal class AzureDevopsFields
    {
        public const string WorkItemId = "System.Id";
        public const string WorkItemType = "System.WorkItemType";
        public const string WorkItemTitle = "System.Title";
        public const string WorkItemState = "System.State";
        public const string WorkItemReason = "System.Reason";


        public const string WorkItemAssignedTo = "System.AssignedTo";

        public const string CreatedBy = "System.CreatedBy";
        public const string ChangedBy = "System.ChangedBy";

        public const string CreatedDate = "System.CreatedDate";
        public const string ChangedDate = "System.ChangedDate";

        public const string TeamProject = "System.TeamProject";
        public const string WorkItemParent = "System.Parent";

        public const string Tags = "System.Tags";

        public const string AreaPath = "System.AreaPath";
        public const string IterationPath = "System.IterationPath";

        // Misc useful
        public const string BoardColumn = "System.BoardColumn";
        public const string BoardColumnDone = "System.BoardColumnDone";

        //Description & rich text
        public const string Description = "System.Description";
        public const string AcceptanceCriteria = "Microsoft.VSTS.Common.AcceptanceCriteria";
        public const string ReproSteps = "Microsoft.VSTS.TCM.ReproSteps";
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