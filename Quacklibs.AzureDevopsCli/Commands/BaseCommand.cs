using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli.Commands;

public abstract class BaseCommand : System.CommandLine.Command
{
    protected readonly SettingsService SettingsService;
    protected readonly Settings Settings;

    protected BaseCommand(string commandName, string description, params string[] aliasses) : base(commandName, description)
    {
        if (aliasses.Any())
        {
            base.Aliases.AddRange(aliasses);
        }

        SettingsService = Program.ServiceLocator.GetService<SettingsService>()!;
        Settings = SettingsService!.Settings;

        this.SetAction(async context =>
        {
            await OnExecuteAsync(context);
        });
    }

    protected virtual Task<int> OnExecuteAsync(ParseResult parseResult)
    {
        Console.WriteLine("no parameter provided. append --help to the command see the available options");
        return Task.FromResult(ExitCodes.Ok);
    }

}