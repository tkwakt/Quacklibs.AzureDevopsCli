namespace Quacklibs.AzureDevopsCli.Presentation.Tables;


public class TableColor
{
    public string Value;

    private TableColor(string value)
    {
        this.Value = value;
    }

    public static TableColor White => new("[white]");
    public static TableColor Skyblue => new("[skyblue]");


    public string ToMarkup(string text) => $"{Value}{text}[/]";
}

