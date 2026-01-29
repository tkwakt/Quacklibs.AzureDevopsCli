using Microsoft.VisualStudio.Services.WebApi;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{
    public static class WorkItemLink
    {
        public static string ToWorkItemLink(this WorkItemLinkType type)
            => type.ToWorkItemUrl().AsUrlMarkup("link");

        public static string ToWorkItemUrl(this WorkItemLinkType type)
           => $"{type.organizationUrl}/_workitems/edit/{type.workItemId}";
    }

    public static class AzureDevopsField
    {
        public static string TryGetFieldValue(this IDictionary<string, object> fields, string key, string defaultValue)
        {
            fields.TryGetValue(key, out var result);

            if (key == AzureDevopsFields.WorkItemAssignedTo)
            {
                var assignedToValue = result as IdentityRef;
                return assignedToValue == null ? defaultValue : assignedToValue.DisplayName;
            }

            return result == null ? defaultValue : result.ToString();
        }
    }
}