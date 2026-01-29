using Spectre.Console.Rendering;

namespace Quacklibs.AzureDevopsCli.Core.Behavior.Console.Presentation.Tables;

public enum ColumnValueAlignment
{
    Left,
    Center,
    Right
}

public class ColumnValue<T>
{
    private readonly Func<T, string?> _columnValueSelector;
    private readonly Func<T, ColumnValueAlignment> _justificationSelector;
    private readonly TableColor _color;
    private readonly bool _isMarkup;

    public ColumnValue(Func<T, string?> columnValueSelector)
    {
        _columnValueSelector = columnValueSelector;
        //TODO: this default assumes that the user has an black console. 
        _color = TableColor.White;
        _justificationSelector = (_) => ColumnValueAlignment.Left;
    }

    public ColumnValue(Func<T, string?> columnValueSelector, TableColor color) : this(columnValueSelector)
    {
        _color = color;
    }

    public ColumnValue(Func<T, string?> columnValueSelector, Func<T, ColumnValueAlignment> justificationSelector) : this(columnValueSelector)
    {
       _justificationSelector = justificationSelector;
    }

    public string ToString(T value)
    {
        var columnValue = _columnValueSelector(value) ?? string.Empty;
        bool isMarkup = columnValue.EndsWith("/]");
        var safeColumnValue = isMarkup ? columnValue : Markup.Escape(columnValue);

        return safeColumnValue;
    }

    public IRenderable Render(T value)
    {
        var columnValue = _columnValueSelector(value) ?? string.Empty;
        bool isMarkup = columnValue.EndsWith("/]");
        var safeColumnValue = isMarkup ? columnValue : Markup.Escape(columnValue);

        var coloredTableText = _color.ToMarkup(safeColumnValue);

        var markup = new Markup(safeColumnValue);
        var alignment = _justificationSelector(value);

        var align = alignment switch
        {
            ColumnValueAlignment.Right => Justify.Right,
            ColumnValueAlignment.Center => Justify.Center,
            ColumnValueAlignment.Left => Justify.Left,
            _ => Justify.Left
        };

        markup.Justify(align);
        return markup;

    }
}
