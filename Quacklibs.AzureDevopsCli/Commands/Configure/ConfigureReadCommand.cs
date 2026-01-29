using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli.Commands.Configure
{
    internal class ConfigureReadCommand : BaseCommand
    {
        public static string CommandHelpText => $"run '{CommandConstants.BaseCommand} configure {CommandConstants.ReadCommand}' to read the current configuration";

        public ConfigureReadCommand() : base(CommandConstants.ReadCommand, "Read the current configuration")
        {
        }

        protected override async Task<int> OnExecuteAsync(ParseResult context)
        {
            try
            {
                var configOptions = base.Settings.GetDisplayableConfig();
                var title = $"Configuration".EscapeMarkup();
                var table = TableBuilder<AppOptionKeyValue>
                            .Create()
                            .WithTitle(title)
                            .WithColumn(name: "Name", valueSelector: new(e => e.Name))
                            .WithColumn(name: "Value", valueSelector: new(e => e.Value?.ToString()))
                            .WithRows(configOptions)
                            .WithOptions(e => e.LeftAligned())
                            .Build();

                AnsiConsole.Write(table);
            }
            catch (Exception exception)
            {
                AnsiConsole.WriteException(exception);
                return ExitCodes.Error;
            }

            return ExitCodes.Ok;
        }
    }
}