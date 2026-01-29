using Microsoft.Extensions.DependencyInjection;
using Quacklibs.AzureDevopsCli.Commands.Configure;
using Quacklibs.AzureDevopsCli.Commands.Daily;
using Quacklibs.AzureDevopsCli.Commands.Project;
using Quacklibs.AzureDevopsCli.Commands.PullRequests;
using Quacklibs.AzureDevopsCli.Commands.SprintPlanning;
using Quacklibs.AzureDevopsCli.Commands.WorkItems;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Presentation;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli
{
    internal class Program
    {
        internal static ServiceProvider ServiceLocator;

        public static int Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddSingleton<SettingsService>()
                .AddScoped<AzureDevopsService>()
                .AddScoped<AzureDevopsUserService>()
                .AddScoped<ICredentialStorage, CredentialStorage>()
                .AddTransient<ConfigureCommand>()
                  .AddTransient<ConfigureReadCommand>()
                .AddTransient<DailyCommand>()
                .AddTransient<WorkItemCommand>()
                  .AddTransient<WorkItemCreateCommand>()
                  .AddTransient<WorkItemReadCommand>()
                  .AddTransient<WorkItemOpenCommand>()
                  .AddTransient<WorkItemUpdateCommand>()
                .AddTransient<ProjectCommand>()
                  .AddTransient<ProjectReadCommand>()
                .AddTransient<PullRequestCommand>()
                  .AddTransient<PullRequestOpenCommand>()
                  .AddTransient<PullRequestReadCommand>()
                .AddTransient<SprintPlanningCommand>()
                 .AddTransient<SprintPlanningUpdateCommand>()
                .AddTransient<DailyCommand>()
                .BuildServiceProvider();

            ServiceLocator = services;

            // Root command
            var root = new RootCommand("a toolbox with options to make working with azure devops easier, quicker and better.");

            //register all subcommands. The actions are registered in the subcommands's
            var configureCommand = ServiceLocator.GetService<ConfigureCommand>()!;
            var workItemCommand = ServiceLocator.GetService<WorkItemCommand>()!;
            var pullRequestCommand = ServiceLocator.GetService<PullRequestCommand>()!;
            var dailyCommand = ServiceLocator.GetService<DailyCommand>()!;
            var projectCommand = ServiceLocator.GetService<ProjectCommand>()!;
            var sprintPlanningCommand = ServiceLocator.GetService<SprintPlanningCommand>()!;

            root.Subcommands.Add(configureCommand);
            root.Subcommands.Add(workItemCommand);
            root.Subcommands.Add(pullRequestCommand);
            root.Subcommands.Add(dailyCommand);
            root.Subcommands.Add(projectCommand);
            root.Subcommands.Add(sprintPlanningCommand);

            ParseResult parseResult = root.Parse(args);

            if (parseResult.Errors.Count > 0)
            {
                foreach (var error in parseResult.Errors)
                {
                    AnsiConsole.MarkupLine($"Error: {error.Message}".WithErrorMarkup());
                }
                return ExitCodes.Error;
            }

            return parseResult.Invoke();
        }
    }
}