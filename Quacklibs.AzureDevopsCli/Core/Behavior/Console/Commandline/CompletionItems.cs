using System.CommandLine.Completions;

namespace Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline
{


    public class EnumCompletionItem : CompletionItem
    {
        public EnumCompletionItem(string label, string kind = "Value", string? sortText = null, string? insertText = null, string? documentation = null, string? detail = null) : base(label, kind, sortText, insertText, documentation, detail)
        {
        }
    }

    /// <summary>
    /// Helper for command line completions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class CompletiontionItems
    {
        public static IEnumerable<CompletionItem> FromEnum<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).Select(e => new EnumCompletionItem(e));
        }
    }
}
