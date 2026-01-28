using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Users;
using Microsoft.VisualStudio.Services.WebApi;
using Spectre.Console;

namespace Quacklibs.AzureDevopsCli.Services
{
    internal class AzureDevopsUserService
    {
        private readonly AzureDevopsService _service;

        public AzureDevopsUserService(AzureDevopsService service)
        {
            _service = service;
        }

        public async Task<AzureDevopsUserType> GetOrSelectUserAsync(string? searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                return new NoAzureDevopsUserFound(string.Empty);
            }

            var availableUsers = await _service.GetClient<GraphHttpClient>().ListUsersAsync();

            var queryAbleUsers = availableUsers.GraphUsers.Where(u => !string.IsNullOrEmpty(u.MailAddress)).ToList();

            var users = queryAbleUsers.Where(e => e.MailAddress.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                                                  e.DisplayName.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                                                  e.PrincipalName.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase));

            if (!users.Any())
            {
                AnsiConsole.WriteLine($"No user found for {searchQuery}");
                return new NoAzureDevopsUserFound(searchQuery);
            }
            if (users.Count() == 1)
            {
                var user = users.First();
               return await ToAzureDevopsUser(user);
            }
            else
            {
                Func<GraphUser, string> displayString = e => $"{e.DisplayName} {e.MailAddress}";

                var userPrompt = new SelectionPrompt<GraphUser>()
                    .Title("Multipe possible users found. Select one")
                    .PageSize(100)
                    .AddChoices(users.ToArray())
                    .UseConverter(displayString);

                var userChoice = AnsiConsole.Prompt(userPrompt);

                if (userChoice == null)
                    return new NoAzureDevopsUserFound(searchQuery);

                return await ToAzureDevopsUser(userChoice);
            }
        }

        /// <summary>
        /// Convert the Graph API user to an AzureDevopsUserType. This involves resolving the Graph Descriptor to an actual azure devops user ID via the Identity API
        /// </summary>
        /// <param name="graphUser"></param>
        /// <returns></returns>
        private async Task<AzureDevopsUserType> ToAzureDevopsUser(GraphUser graphUser)
        {
            //var identityClient = _service.GetClient<IdentityHttpClient>();

            var graphDescriptor = graphUser.Descriptor.Identifier;
            var userId = Guid.TryParse(graphDescriptor, out var parsedGuid) ? parsedGuid : Guid.Empty;

            //var identities = await identityClient.ReadIdentitiesAsync(
            //    new[]
            //    {
            //        new IdentityDescriptor(graphUser.Descriptor.SubjectType, graphUser.Descriptor.Identifier)
            //    },
            //    QueryMembership.None
            //);

            //var userId = identities.Single().Id;
            
            return new AzureDevopsUserType(userId, graphUser.MailAddress, graphUser.DisplayName);

        }
    }
}