namespace Quacklibs.AzureDevopsCli.Core.Behavior.Console.Presentation.Tables;

/// <summary>
/// Easily create tables. This objects ensures that all our tables will look the same. 
/// </summary>
/// <typeparam name="T"></typeparam>
public class TableBuilder<T> : ISubColumnBuilder<T>, ITableColumnBuilder<T>, ITableBuilderStarter<T>
{
    private readonly Table _table;
    private readonly List<T> _rows = [];
    private readonly List<ColumnValue<T>> _columnValues = [];

    private string _title { get; set; }

    private TableBuilder()
    {
        _table = new Table()
                 .Border(TableBorder.Minimal)
                 .BorderColor(Color.Grey);
    }

    public static ITableBuilderStarter<T> Create() => new TableBuilder<T>();

    public ITableColumnBuilder<T> WithTitle(string Title)
    {
        _title = Title;
        return this;
    }

    public ISubColumnBuilder<T> WithColumn(string name, ColumnValue<T> valueSelector)
    {
        _table.AddColumn(name);
        _columnValues.Add(valueSelector);

        return this;
    }

    public ISubColumnBuilder<T> WithColumn(string name, string value)
    {
        _table.AddColumn(name);
        _columnValues.Add(new ColumnValue<T>(_ => value));

        return this;
    }

    public ISubColumnBuilder<T> WithSubColumn(string name, ColumnValue<T> valueSelector)
    {
        throw new NotImplementedException();
    }

    public ITableBuilder<T> WithRows(IEnumerable<T> rows)
    {
        foreach (var item in rows)
        {
            _rows.Add(item);
        }

        return this;
    }

    public ITableBuilder<T> WithOptions(Action<Table> dostuff)
    {
        dostuff(_table);
        return this;
    }

    public Table Build()
    {

        _table.Title = new TableTitle(_title.EscapeMarkup(), new Style(decoration: Decoration.Bold | Decoration.Underline));

        foreach (var row in _rows)
        {
            var columnValues = _columnValues.Select(columnValues => columnValues.Render(row))
                                            .ToArray();
            _table.AddRow(columnValues);
        }

        return _table;
    }


}

public interface ITableBuilderStarter<T>
{
    public ITableColumnBuilder<T> WithTitle(string Title);
}


public interface ITableColumnBuilder<T>
{
    public ISubColumnBuilder<T> WithColumn(string name, ColumnValue<T> valueSelector);
}

public interface ITableBuilder<T> : ITableColumnBuilder<T>
{
    public ITableBuilder<T> WithRows(IEnumerable<T> rows);
    public ITableBuilder<T> WithOptions(Action<Table> dostuff);
    public Table Build();
}

public interface ISubColumnBuilder<T> : ITableBuilder<T>
{
    public ISubColumnBuilder<T> WithSubColumn(string name, ColumnValue<T> valueSelector);
}