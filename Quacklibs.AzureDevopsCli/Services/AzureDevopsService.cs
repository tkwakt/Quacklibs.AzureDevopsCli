using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Quacklibs.AzureDevopsCli.Commands.Configure;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli
{
    public class AzureDevopsService
    {
        private readonly SettingsService _settingsService;
        private Lazy<VssConnection> _connection
            => new(() => CreatePATConnection(_settingsService.Settings.OrganizationUrl, _settingsService.Settings.PAT));


        public AzureDevopsService()
        {
            _settingsService = Program.ServiceLocator.GetService<SettingsService>()!;
        }


        public T GetClient<T>() where T : IVssHttpClient
        {
            if (string.IsNullOrEmpty(_settingsService.Settings.OrganizationUrl))
            {
                throw new ArgumentException($"Unable to create a connection, no {nameof(Settings.OrganizationUrl)}. Set with {ConfigureCommand.CommandHelpText}");
            }
            if (string.IsNullOrEmpty(_settingsService.Settings.PAT))
            {
                throw new ArgumentException($"Unable to create a connection, no {nameof(Settings.PAT)}. Set with {ConfigureCommand.CommandHelpText}");
            }

            return _connection.Value.GetClient<T>();
        }


        /// <summary>
        /// Personal Access Token authentication (legacy - use modern auth instead)
        /// </summary>
        public static VssConnection CreatePATConnection(string organizationUrl, string personalAccessToken)
        {
            var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
            return new VssConnection(new Uri(organizationUrl), credentials);
        }
    }
}