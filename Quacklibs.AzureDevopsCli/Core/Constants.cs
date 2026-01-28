namespace Quacklibs.AzureDevopsCli.Core
{
    public class CommandConstants
    {
        public const string BaseCommand = "azdo";

        public const string OpenCommand = "open";

        public const string ReadCommand = "read";
        public const string WriteCommand = "write";
        public const string UpdateCommand = "update";
        public const string CreateCommand = "create";

        public const string ProjectOptionTemplate = "-p|--project|--team";
    }

    public class CommandOptionConstants
    {
        public const string ForOptionName = "--for";
        public const string SinceOptionName = "--since";
        public static string[] SinceOptionAliasses = ["-s"];

    }
}