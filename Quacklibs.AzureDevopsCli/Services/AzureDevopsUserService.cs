using Microsoft.VisualStudio.Services.Graph.Client;
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
                AnsiConsole.WriteLine($"No user found for {searchQuery} we continue anyway.");
                return new NoAzureDevopsUserFound(searchQuery);
            }
            if (users.Count() == 1)
            {
                var user = users.First();
                return new AzureDevopsUserType(user.OriginId, user.MailAddress, user.DisplayName);
            }
            else
            {
                var selectableUsers = users.Select(usr => new AzureDevopsUserType(usr.OriginId, usr.MailAddress, usr.DisplayName));

                Func<AzureDevopsUserType, string> displayString = e => $"{e.DisplayName} {e.Email}";

                var userPrompt = new SelectionPrompt<AzureDevopsUserType>()
                    .Title("Multipe possible users found. Select one")
                    .PageSize(100)
                    .AddChoices(selectableUsers.ToArray())
                    .UseConverter(displayString);

                var userChoice = AnsiConsole.Prompt(userPrompt);

                return userChoice;
            }
        }
    }
}