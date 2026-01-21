using System.Text.RegularExpressions;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{
    public static class HtmlContent
    {
        /// <summary>
        /// Convert basic HTML tags with it's spectre console's equivalent.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string ToSpectreConsoleMarkup(this HtmlContentType content)
        {
            if (string.IsNullOrEmpty(content.Value))
                return string.Empty;

            var value = content.Value;

            // handle lists
            value = Regex.Replace(value, @"<ul[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"</ul>", "\n", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"</li>", "\n", RegexOptions.IgnoreCase);

            // Replace Azure DevOps mentions: <a data-vss-mention>...</a>
            value = Regex.Replace(value, @"<a[^>]*data-vss-mention[^>]*>(.*?)</a>", match => $"{Markup.Escape(match.Groups[1].Value.Trim())}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Images -> **image**
            value = Regex.Replace(value, @"<img[^>]*>", "*image stripped*",  RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return value
                .Replace("<b>", "[bold]").Replace("</b>", "[/]")
                .Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n")
                .Replace("<strong>", "[bold]").Replace("</strong>", "[/]")
                .Replace("<i>", "[italic]").Replace("</i>", "[/]")
                .Replace("<em>", "[italic]").Replace("</em>", "[/]")
                .Replace("<u>", "[underline]").Replace("</u>", "[/]")
                .Replace("<div>", "").Replace("</div>", "")
                .Replace("<div>", "").Replace("</div>", "")

          
                .Pipe(s => Regex.Replace(s, @"<a\s+href\s*=\s*""([^""]+)""\s*>(.*?)</a>",
                      match => $"[link={match.Groups[1].Value}]{match.Groups[2].Value}[/]")); ;
        }

        private static string Pipe(this string input, Func<string, string> func) => func(input);
    }

}