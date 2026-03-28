using System;

namespace Quacklibs.AzureDevopsCli.Core.Types.Workitem;

public static class RenderableExtensions
{
    public static Text AsRenderable(this string text)
    {
        return new Text(text);
    }
}
