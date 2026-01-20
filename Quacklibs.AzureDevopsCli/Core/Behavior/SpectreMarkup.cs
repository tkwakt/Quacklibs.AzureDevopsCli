using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{
    public static class SpectreMarkup
    {
        public static string Bold(this string text) => $"[bold]{text}[/]";
        public static string Underline(this string text) => $"[underline]{text}[/]";
        public static string Italic(this string text) => $"[italic]{text}[/]";

        public static string Highlight(this string text) => $"[black on yellow]{text}[/]";

        public static string WithWarningMarkup(this string text) => $"[yellow]{text}[/]"; 
        public static string WithErrorMarkup(this string text) => $"[red]{text}[/]";
        public static string WithSuccessMarkup(this string text) => $"[green]{text}[/]";
    }
}
