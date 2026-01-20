
using Quacklibs.AzureDevopsCli.Core;

namespace Quacklibs.AzureDevopsCli.Commands.Configure
{
    internal class ConfigureCommand : BaseCommand
    {
        public static string CommandHelpText => $"{CommandConstants.BaseCommand} configure";
        public ConfigureCommand(ConfigureReadCommand configureReadCommand) : base("configure", "Configure")
        {
            this.Subcommands.Add(configureReadCommand);
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var currentOrganizationUrl = base.Settings.OrganizationUrl ?? "n/a";
            Console.WriteLine("Set the Azure DevOps organization URL (e.g., https://dev.azure.com/yourorganization)\" ");
            Console.WriteLine($"\n Current: {currentOrganizationUrl} Input new value. Press enter to skip");

            var newOrganizationUrl = Console.ReadLine();

            if (!string.IsNullOrEmpty(newOrganizationUrl))
            {
                base.Settings.OrganizationUrl = newOrganizationUrl;
                base.SettingsService.Save(base.Settings);
                Console.WriteLine("Organization URL updated.");
            }
            else
            {
                Console.WriteLine("No changes made to Organization URL.");
            }

            var currentProject = base.Settings.DefaultProject ?? "n/a";
            Console.WriteLine("\nSet the default Azure DevOps project:");
            Console.WriteLine($"\n Current: {currentProject} Input new value. Press enter to skip");
            var newProject = Console.ReadLine();

            if (!string.IsNullOrEmpty(newProject))
            {
                base.Settings.DefaultProject = newProject;
                base.SettingsService.Save(base.Settings);
                Console.WriteLine("Default project updated.");
            }
            else
            {
                Console.WriteLine("No changes made to default project.");
            }

            var currentPat = base.Settings.PAT != null ? new string('*', base.Settings.PAT.Length) : "n/a";
            Console.WriteLine("\n set the PAT used to access your account");
            Console.WriteLine($"\n Current: {currentPat} Input new value. Press enter to skip");

            var newPat = Console.ReadLine();
            if (!string.IsNullOrEmpty(newPat))
            {
                base.Settings.PAT = newPat;
                base.SettingsService.Save(base.Settings);
                Console.WriteLine("PAT updated.");
            }
            else
            {
                Console.WriteLine("No changes made to PAT.");
            }

            var currentUserEmail = base.Settings.UserEmail;
            Console.WriteLine("\nSet the user email associated with the PAT:");
            Console.WriteLine($"\n Current: {currentUserEmail} Input new value. Press enter to skip");
            var newUserEmail = Console.ReadLine();
            if (!string.IsNullOrEmpty(newUserEmail))
            {
                base.Settings.UserEmail = newUserEmail;
                base.SettingsService.Save(base.Settings);
                Console.WriteLine("User email updated.");
            }
            else
            {
                Console.WriteLine("No changes made to user email.");
            }



            return ExitCodes.Ok;

        }
    }
}