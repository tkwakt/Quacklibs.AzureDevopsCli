namespace Quacklibs.AzureDevopsCli.Core.Behavior.Console.Presentation
{
    public static class SpectreMarkup
    {
        public static string Bold(this string text) => $"[bold]{text}[/]";
        public static string Underline(this string text) => $"[underline]{text}[/]";
        public static string Italic(this string text) => $"[italic]{text}[/]";

        public static string Highlight(this string text) => $"[black on white]{text}[/]";

        public static string WithWarningMarkup(this string text) => $"[yellow]{text}[/]"; 
        public static string WithErrorMarkup(this string text) => $"[red]{text}[/]";
        public static string WithSuccessMarkup(this string text) => $"[green]{text}[/]";

        public static string AsWorkItemStateMarkup(this string state) => state switch
        {
            "New" => "[blue]New[/]",
            "Active" => "[yellow]Active[/]",
            "Resolved" => "[green]Resolved[/]",
            "Closed" => "[green]Closed[/]",
            _ => state
        };
    }



    public static class AnsiConsoleExtensions
    {
        public static string AsUrlMarkup(this string url, string displayText = "link")
            => $"[Link={url}]{displayText}[/]";
    }
}
