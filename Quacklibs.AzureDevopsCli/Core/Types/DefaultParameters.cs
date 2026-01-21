using Quacklibs.AzureDevopsCli.Core.Behavior;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli.Core.Types
{
    public class Settings
    {
        public string OrganizationUrl { get; set; }

        public string DefaultProject { get; set; }

        public string PAT { get; set; }

        public string UserEmail { get; set; }

        public List<AppOptionKeyValue> GetDisplayableConfig()
        {
            var props = this.GetType().GetProperties();
            var result = new List<AppOptionKeyValue>();

            foreach (var prop in props)
            {
                var value = prop.GetValue(this);

                if (prop.Name == nameof(PAT))
                    result.Add(new AppOptionKeyValue(prop.Name, "***************"));
                else
                    result.Add(new AppOptionKeyValue(prop.Name, value?.ToString()));
            }

            return result;
        }
    }
}