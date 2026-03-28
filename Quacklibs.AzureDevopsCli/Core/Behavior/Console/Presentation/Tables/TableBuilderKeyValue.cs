using System;

namespace Quacklibs.AzureDevopsCli.Core.Behavior.Console.Presentation.Tables;

public class TableBuilderKeyValue
{
    private readonly Table _table;

    private string _title { get; set; } = String.Empty;

    private TableBuilderKeyValue()
    {
        _table = new Table()
                 .Border(TableBorder.Minimal)
                 .BorderColor(Color.Grey);
    }

    public TableBuilderKeyValue WithTitle(string Title)
    {
        _title = Title;
        return this;
    }

    public TableBuilderKeyValue WithColumn(ColumnValue<string> columnValue)
    {
       // _table.AddColumn();
        return this;
    }

    public TableBuilderKeyValue WithRow(string rowValue)
    {
   //    _table.AddRow(row);
        return this;
    }

}
