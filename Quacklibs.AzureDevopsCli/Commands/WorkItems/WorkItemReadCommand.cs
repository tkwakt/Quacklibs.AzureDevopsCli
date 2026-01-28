using Quacklibs.AzureDevopsCli.Core.Behavior;
using Quacklibs.AzureDevopsCli.Core.Behavior.Commandline;
using Quacklibs.AzureDevopsCli.Services;
using System;
using System.CommandLine;

namespace Quacklibs.AzureDevopsCli.Commands.WorkItems;

internal class WorkItemReadCommand : BaseCommand
{
    private const int MaxAllowableNumbersOfWorkItems = 200;


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

        var complationItems = CompletiontionItems.FromEnum<WorkItemState>().ToArray();
        _stateOption.CompletionSources.Add(ctx => complationItems);

        _forOption.DefaultValueFactory = _ => Settings.UserEmail;

        _azureDevops = azureDevops;
        _azureDevopsUserService = azureDevopsUserService;
    }

    protected override async Task<int> OnExecuteAsync(ParseResult context)
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