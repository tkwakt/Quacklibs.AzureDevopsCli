using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline;
using Quacklibs.AzureDevopsCli.Services;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Linq;

namespace Quacklibs.AzureDevopsCli.Commands.WorkItems;

internal class WorkItemReadCommand : BaseCommand
{
    private const int MaxAllowableNumbersOfWorkItems = 200;
    public const string CommandText = $"{CommandConstants.BaseCommand} workitem {CommandConstants.ReadCommand}";
    public string CommentTextWorkItemReadSingleItem => $"{CommandConstants.BaseCommand} workitem {CommandConstants.ReadCommand} --id IdValue";

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
        string noValue = "-";

        var client = _azureDevops.GetClient<WorkItemTrackingHttpClient>();
        var workItem = await client.GetWorkItemAsync(id: workItemId, expand: WorkItemExpand.Relations);
        var comments = await client.GetCommentsAsync(workItemId);

        //hierarchy
        var workItemHierarchy = await BuildWorkItemHierarchyAsync(client, workItem);

        //Target workitem
        var tableTitle = $"{workItem.Id} {workItem.Fields[AzureDevopsFields.WorkItemTitle]}";

        var state = GetField(AzureDevopsFields.WorkItemState);
        var description = new HtmlContentType(GetField(AzureDevopsFields.Description)).ToSpectreConsoleMarkup();
        var acceptanceCriteria = new HtmlContentType(GetField(AzureDevopsFields.AcceptanceCriteria)).ToSpectreConsoleMarkup();
        var assignedTo = noValue;
        if (workItem.Fields.TryGetValue(AzureDevopsFields.WorkItemAssignedTo, out var result))
        {
            if (result is IdentityRef identity)
            {
                assignedTo = identity.DisplayName;
            }
        }

        var metaTable = new Table().Border(TableBorder.Minimal);

        metaTable.AddColumn($"Field");
        metaTable.AddColumn("Value");

        metaTable.AddRow("Title", tableTitle.EscapeMarkup());
        metaTable.AddRow("Type", GetField(AzureDevopsFields.WorkItemType));
        metaTable.AddRow("State", state.AsWorkItemStateMarkup());
        metaTable.AddRow("Assigned To", assignedTo);
        metaTable.AddRow("Area", GetField(AzureDevopsFields.AreaPath));
        metaTable.AddRow("Iteration", GetField(AzureDevopsFields.IterationPath));
        metaTable.AddRow("Tags", GetField(AzureDevopsFields.Tags));
        metaTable.AddRow("Created", GetField(AzureDevopsFields.CreatedDate));
        metaTable.AddRow("Updated", GetField(AzureDevopsFields.ChangedDate));
        metaTable.AddRow("Description", description.EscapeMarkup());

        var acceptancePanel = new Panel(new Markup(acceptanceCriteria))
        {
            Header = new PanelHeader("Acceptance Criteria"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(workItemHierarchy);
        AnsiConsole.Write(metaTable);

        if (acceptanceCriteria != noValue)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(acceptancePanel);
        }

        if (comments.Count > 0)
        {
            var commentsTree = new Tree("comments");
            var workItemComments = comments.Comments.Select(e => new Core.Types.Workitem.WorkItemComment(e.RevisedDate, e.Text, e.RevisedBy.DisplayName).DisplayText);
            commentsTree.AddNodes(workItemComments);

            AnsiConsole.Write(commentsTree);
        }

        AnsiConsole.WriteLine();

        return ExitCodes.Ok;

        string GetField(string fieldName) => workItem.Fields.ContainsKey(fieldName) ? workItem.Fields[fieldName]?.ToString() ?? noValue : noValue;
    }


    private async Task<IRenderable> BuildWorkItemHierarchyAsync(WorkItemTrackingHttpClient client, WorkItem targetWorkItem)
    {
        var parentWorkItems = await GetOrderedParentsAsync(client, targetWorkItem);

        //todo: add entire child tree. not only the first level
        var childRelationWorkItemId = targetWorkItem.Relations
                                                    .Where(r => r.Rel == "System.LinkTypes.Hierarchy-Forward")
                                                    .Select(e => ExtractIdFromUrl(e.Url))
                                                    .ToList();

        var childWorkItems = childRelationWorkItemId.Any() ? await client.GetWorkItemsAsync(childRelationWorkItemId) : [];

        var tree = new Tree("");
        tree.AddNode(new Markup("Workitem hierarchy"));
        TreeNode leaf = tree.Nodes[0];

        foreach (var wi in parentWorkItems)
        {
            leaf = leaf.AddNode(new TreeNode(new Markup(GetWorkItemNode(wi))));
        }

        var childWorkItemNodes = childWorkItems.Select(wi => GetWorkItemNode(wi, true));

        leaf.AddNode(GetWorkItemNode(targetWorkItem).WithWarningMarkup())
            .AddNodes(childWorkItemNodes);

        return tree;


        string GetWorkItemNode(WorkItem? workItem, bool format = false)
        {
            if (workItem == null)
                return "N/A";

            var wTitle = workItem.Fields[AzureDevopsFields.WorkItemTitle]?.ToString() ?? "-";
            var wType = workItem.Fields[AzureDevopsFields.WorkItemType]?.ToString() ?? "-";
            var wState = workItem.Fields[AzureDevopsFields.WorkItemState]?.ToString() ?? "-";

            return format ? $"{workItem.Id,-8} {wType,-12} {wState.AsWorkItemStateMarkup(),-17} {wTitle.EscapeMarkup()}"
                          : $"{workItem.Id} - {wType} - {wState.AsWorkItemStateMarkup()} - {wTitle.EscapeMarkup()}";
        }
    }

    private async Task<List<WorkItem>> GetOrderedParentsAsync(WorkItemTrackingHttpClient client, WorkItem workItem)
    {
        var parents = new List<WorkItem>();
        var current = workItem;

        while (true)
        {
            var parentId = current.Relations?.FirstOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse")?.Url is string url
                    ? ExtractIdFromUrl(url) : (int?)null;

            if (parentId == null)
                break;


            var parent = await client.GetWorkItemAsync(id: parentId.Value, expand: WorkItemExpand.Relations);

            if (parent == null)
                break;

            parents.Add(parent);
            current = parent;
        }

        //ensure that oldest parent is first in the list
        parents.Reverse();

        return parents;
    }

    /// <summary>
    /// The relation's URL looks like: "https://dev.azure.com/{org}/{project}/_apis/wit/workItems/{id}"
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    int ExtractIdFromUrl(string url) => int.Parse(url.Split('/').Last());

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

        AnsiConsole.MarkupLine($"Use '{WorkItemOpenCommand.CommandHelpText.WithWarningMarkup()}' to open workitem in browswer \n");
        AnsiConsole.MarkupLine($"Use '{CommentTextWorkItemReadSingleItem.WithWarningMarkup()}' to open workitem details \n");

        return ExitCodes.Ok;
    }
}