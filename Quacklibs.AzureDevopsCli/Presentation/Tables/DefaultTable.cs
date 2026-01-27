namespace Quacklibs.AzureDevopsCli.Presentation.Tables;

/// <summary>
/// Easily create tables. This objects ensures that all our tables will look the same. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class DefaultTable<T>
{
    private readonly Table _table;
    private readonly List<T> _rows = [];
    private readonly List<ColumnValue<T>> _columnValues = [];

    public DefaultTable()
    {
        _table = new Table()
            .Border(TableBorder.Minimal)
            .BorderColor(Color.Grey);
    }

    public DefaultTable<T> WithColumn(string name, ColumnValue<T> valueSelector)
    {
        _table.AddColumn(name);
        _columnValues.Add(valueSelector);

        return this;
    }

    public DefaultTable<T> WithRows(IEnumerable<T> rows)
    {
        foreach (var item in rows)
        {
            _rows.Add(item);
        }
        return this;
    }

    public DefaultTable<T> WithOptions(Action<Table> dostuff)
    {
        dostuff(_table);
        return this;
    }

    public Table Build()
    {
        foreach (var row in _rows)
        {
            var columnValues = _columnValues.Select(columnValues => columnValues.ToString(row))
                                            .ToArray();

            _table.AddRow(columnValues);
        }

        return _table;
    }
}