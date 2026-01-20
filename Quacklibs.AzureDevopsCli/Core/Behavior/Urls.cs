using System.Runtime.InteropServices;

namespace Quacklibs.AzureDevopsCli.Core.Behavior
{
    public static class Urls
    {
        public static string Encode(this string input) => Uri.EscapeDataString(input);

        static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw new NotSupportedException("Unsupported OS platform");
            }
        }
    }
}