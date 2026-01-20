namespace Quacklibs.AzureDevopsCli.Presentation.Tables;

public class ColumnValue<T>
{
    private readonly Func<T, string?> _columnValueSelector;
    private readonly TableColor _color;
    private readonly bool _isMarkup;

    public ColumnValue(Func<T, string?> columnValueSelector)
    {
        _columnValueSelector = columnValueSelector;
        //TODO: this default assumes that the user has an black console. 
        _color = TableColor.White;
    }

    public ColumnValue(Func<T, string?> columnValueSelector, TableColor color) : this(columnValueSelector)
    {
        _color = color;
    }

    public string ToString(T value)
    {
        var columnValue = _columnValueSelector(value) ?? string.Empty;
        bool isMarkup = columnValue.EndsWith("/]");
        var safeColumnValue = isMarkup ? columnValue : Markup.Escape(columnValue);

        return safeColumnValue;
    }

}