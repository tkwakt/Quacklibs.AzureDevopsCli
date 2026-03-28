using System.Reflection;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.Process;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline;
using Quacklibs.AzureDevopsCli.Core.Types.Workitem;
using Quacklibs.AzureDevopsCli.Services;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Quacklibs.AzureDevopsCli.Commands.WorkItems;

internal class WorkItemReadCommand : BaseCommand
{
    private const int MaxAllowableNumbersOfWorkItems = 200;
    public const string CommandText = $"{CommandConstants.BaseCommand} workitem {CommandConstants.ReadCommand}";

    private Option<int> _workItemIdOption = new("--id")
    {
        Required = false
    };

    private Option<string> _forOption = new(CommandOptionConstants.ForOptionName);

    private Option<WorkItemState[]> _stateOption = new("--state")
    {
        Arity = ArgumentArity.OneOrMore,
        DefaultValueFactory = (_) => [WorkItemState.New, WorkItemState.Active]
    };

    private readonly AzureDevopsService _azureDevops;
    private readonly AzureDevopsUserService _azureDevopsUserService;

    public WorkItemReadCommand(AzureDevopsService azureDevops, AzureDevopsUserService azureDevopsUserService) : base(CommandConstants.ReadCommand, "Read work items assigned to a user")
    {
        Options.Add(_forOption);
        Options.Add(_stateOption);
        Options.Add(_workItemIdOption);

        var complationItems = CompletiontionItems.FromEnum<WorkItemState>().ToArray();
        _stateOption.CompletionSources.Add(ctx => complationItems);

        _forOption.DefaultValueFactory = _ => Settings.UserEmail;

        _azureDevops = azureDevops;
        _azureDevopsUserService = azureDevopsUserService;
    }

    protected override async Task<int> OnExecuteAsync(ParseResult context)
    {
        var workItemId = context.GetValue(_workItemIdOption);

        return workItemId != default ? await ReadSingleWorkItemAsync(workItemId)
                                     : await ReadMultipleWorkItemsAsync(context);
    }

    private async Task<int> ReadSingleWorkItemAsync(int workItemId)
    {
        var client = _azureDevops.GetClient<WorkItemTrackingHttpClient>();

        var workItem = await client.GetWorkItemAsync(id: workItemId, expand: WorkItemExpand.Relations);

        var parentRelationWorkItemId = workItem.Relations?.Where(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse")?.Select(e => ExtractIdFromUrl(e.Url)).ToList() ?? [];
        var childRelationWorkItemId = workItem.Relations?.Where(r => r.Rel == "System.LinkTypes.Hierarchy-Forward")?.Select(e => ExtractIdFromUrl(e.Url)).ToList() ?? [];

        var relatedIds = parentRelationWorkItemId.Concat(childRelationWorkItemId).ToList() ?? [];

        var relatedWorkItems = relatedIds.Any()
            ? await client.GetWorkItemsAsync(relatedIds)
            : [];

        var relatedLookup = relatedWorkItems.ToDictionary(w => w.Id);

        var tableTitle = $"{workItem.Id} {workItem.Fields[AzureDevopsFields.WorkItemTitle]}";

        var title = GetField(AzureDevopsFields.WorkItemTitle);
        var type = GetField(AzureDevopsFields.WorkItemType);
        var state = GetField(AzureDevopsFields.WorkItemState);
        var assignedTo = GetField(AzureDevopsFields.WorkItemAssignedTo);
        var area = GetField(AzureDevopsFields.AreaPath);
        var iteration = GetField(AzureDevopsFields.IterationPath);

        var tags = GetField(AzureDevopsFields.Tags);
        var createdDate = GetField(AzureDevopsFields.CreatedDate);
        var changedDate = GetField(AzureDevopsFields.ChangedDate);

        var description = new HtmlContentType(GetField(AzureDevopsFields.Description)).ToSpectreConsoleMarkup();
        var acceptanceCriteria = new HtmlContentType(GetField(AzureDevopsFields.AcceptanceCriteria)).ToSpectreConsoleMarkup();
        var header = new Panel($"[bold underline white]{workItem.Id} {title.EscapeMarkup()}[/]")
        {
            Border = BoxBorder.None,
            Padding = new Padding(1, 1)
        };

        var metaTable = new Table().Border(TableBorder.Minimal);
        metaTable.AddColumn($"Field");
        metaTable.AddColumn("Value");

        metaTable.AddRow("Type", type);
        metaTable.AddRow("State", $"[bold]{ColorState(state)}[/]");
        metaTable.AddRow("Assigned To", assignedTo);
        metaTable.AddRow("Area", area);
        metaTable.AddRow("Iteration", iteration);
        metaTable.AddRow("Tags", tags);
        metaTable.AddRow("Created", createdDate);
        metaTable.AddRow("Updated", changedDate);
        metaTable.AddRow("Description", description);


        // --- Parent panel ---
        IRenderable parentPanel = BuildParentTable();

        // --- Children table ---
        IRenderable childrenTable = BuildChildrenTable();

        var acceptancePanel = new Panel(new Markup(acceptanceCriteria))
        {
            Header = new PanelHeader("Acceptance Criteria"),
            Border = BoxBorder.Rounded
        };

        var relationsCount = workItem.Relations?.Count ?? 0;
        var relationsPanel = new Panel($"[yellow]{relationsCount} linked items[/]")
        {
            Header = new PanelHeader("Relations"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(header);
        AnsiConsole.Write(metaTable);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(parentPanel);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(childrenTable);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(descriptionPanel);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(acceptancePanel);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(relationsPanel);

        return ExitCodes.Ok;

        string GetField(string fieldName) => workItem.Fields.ContainsKey(fieldName) ? workItem.Fields[fieldName]?.ToString() ?? "-" : "-";
        // The relation's URL looks like: "https://dev.azure.com/{org}/{project}/_apis/wit/workItems/{id}"
        int ExtractIdFromUrl(string url) => int.Parse(url.Split('/').Last());

        IRenderable BuildParentTable()
        {
            var parentId = parentRelationWorkItemId.FirstOrDefault();
            bool hasParent = relatedLookup.TryGetValue(parentId, out WorkItem? parent);

            string parentTItle = string.Empty;

            if (!hasParent)
                parentTItle = "n/a";
            else
            {
                var pTitle = parent.Fields[AzureDevopsFields.WorkItemTitle]?.ToString() ?? "-";
                var pType = parent.Fields[AzureDevopsFields.WorkItemType]?.ToString() ?? "-";
                var pState = parent.Fields[AzureDevopsFields.WorkItemState]?.ToString() ?? "-";

                parentTItle = $"{parentId} - {pTitle} - {pState} - {pType}";
            }

            string 
        }

        string ColorState(string state) => state switch
        {
            "New" => "[blue]New[/]",
            "Active" => "[yellow]Active[/]",
            "Resolved" => "[green]Resolved[/]",
            "Closed" => "[grey]Closed[/]",
            _ => state
        };


        IRenderable BuildChildrenTable()
        {
            var table = new Table()
                .Border(TableBorder.Minimal)
                .Title("Children");

            table.AddColumn("Id");
            table.AddColumn("Type");
            table.AddColumn("State");
            table.AddColumn("Title");

            if (!childRelationWorkItemId.Any())
            {
                table.AddRow("-", "-", "-", "[white]No children[/]");
                return table;
            }

            foreach (var childId in childRelationWorkItemId)
            {
                if (!relatedLookup.ContainsKey(childId))
                    continue;

                var child = relatedLookup[childId];

                var cTitle = child.Fields[AzureDevopsFields.WorkItemTitle]?.ToString() ?? "-";
                var cType = child.Fields[AzureDevopsFields.WorkItemType]?.ToString() ?? "-";
                var cState = child.Fields[AzureDevopsFields.WorkItemState]?.ToString() ?? "-";

                table.AddRow(
                    $"[bold]{child.Id}[/]",
                    cType,
                    ColorState(cState),
                    Markup.Escape(cTitle));
            }

            return table;
        }

    }


    private async Task<int> ReadMultipleWorkItemsAsync(ParseResult context)
    {

        var states = context.GetValue(_stateOption) ?? [];
        var forUser = context.GetValue(_forOption);

        if (forUser != Settings.UserEmail)
        {
            var user = await _azureDevopsUserService.GetOrSelectUserAsync(forUser);

            if (user is NoAzureDevopsUserFound)
            {
                AnsiConsole.MarkupLine($"No user found for '{forUser}'".WithWarningMarkup());
                return ExitCodes.ResourceNoFound;
            }
            else
                forUser = user.Email;
        }

        return await ReadAndDisplayWorkItems(forUser, states);
    }

    public async Task<int> ReadAndDisplayWorkItems(string? assignedTo, WorkItemState[] states)
    {
        assignedTo ??= "@me";

        string stateFilterClause = string.Empty;
        {
            var statesQuoted = states.Select(s => $"'{s}'");
            stateFilterClause = $"AND [System.State] IN ({string.Join(", ", statesQuoted)})";
        }

        var assignedToClause = new AssignedUserWiqlQueryPart(base.Settings.UserEmail).Get(assignedTo);

        var rawQuery = $"""
                                SELECT [System.Id], [System.WorkItemType], [System.Title], [System.State]
                                FROM WorkItems
                                WHERE [System.WorkItemType] IN ('Bug', 'Task', 'User Story')
                                {assignedToClause}
                                {stateFilterClause} 
                                ORDER BY [System.ChangedDate] DESC
                        """;

        var cleanedQuery = string.Join(
           Environment.NewLine,
            rawQuery
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line)));

        var wiql = new Wiql() { Query = cleanedQuery };

        Console.WriteLine($"Querying workitems");

        var result = await _azureDevops.GetClient<WorkItemTrackingHttpClient>().QueryByWiqlAsync(wiql, top: MaxAllowableNumbersOfWorkItems);

        var requestedFields = new[] {
            AzureDevopsFields.WorkItemId,
            AzureDevopsFields.WorkItemType,
            AzureDevopsFields.WorkItemState,
            AzureDevopsFields.WorkItemTitle,
            AzureDevopsFields.TeamProject,
            AzureDevopsFields.IterationPath
        };

        var ids = result.WorkItems.Select(e => e.Id);

        if (!ids.Any())
        {
            AnsiConsole.MarkupLine("No workitems found".WithSuccessMarkup());
            return ExitCodes.Ok;
        }

        var workItems = await _azureDevops.GetClient<WorkItemTrackingHttpClient>()
                                          .GetWorkItemsAsync(ids, fields: requestedFields);

        var table = TableBuilder<WorkItem>
                    .Create()
                    .WithTitle("WorkItems")
                    .WithColumn("id", new(e => e.Id.ToString()))
                    .WithColumn("title", new(e => e.Fields[AzureDevopsFields.WorkItemTitle].ToString()))
                    .WithColumn("work item type", new(e => e.Fields[AzureDevopsFields.WorkItemType].ToString()))
                    .WithColumn("state", new(e => e.Fields[AzureDevopsFields.WorkItemState].ToString()))
                    .WithColumn("iteration", new(e => e.Fields[AzureDevopsFields.IterationPath].ToString()))
                    .WithRows(workItems)
                    .Build();

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine($"Use '{WorkItemOpenCommand.CommandHelpText}' to open workitem in browswer \n");

        return ExitCodes.Ok;
    }
}