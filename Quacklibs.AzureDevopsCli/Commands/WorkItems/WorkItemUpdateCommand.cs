using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli.Commands.WorkItems;

internal class WorkItemUpdateCommand : BaseCommand
{
    private readonly AzureDevopsService _service;
    private readonly AzureDevopsUserService _userService;

    private Option<string> _commentOption = new("--comment", "-c")
    {
        Required = false,
        DefaultValueFactory = _ => string.Empty,
    };

    private readonly Option<string> _forOption = new(CommandOptionConstants.ForOptionName)
    {
        Required = false,
        Description = "Filter the report by person. The value can be an email address or (part of) a name.If multiple users match, an interactive selection is shown.\r\n"
    };

    private Option<int> _workItemIdOption = new("--id");

    public Option<WorkItemState?> _newState = new("--state", "-s")
    {
        Required = false,
        Description = "The new state to set the work item to."
    };

    public WorkItemUpdateCommand(AzureDevopsService service, AzureDevopsUserService userService) : base(CommandConstants.UpdateCommand, "Update a workitem", "u")
    {
        _service = service;
        this._userService = userService;
        var completionSources = CompletiontionItems.FromEnum<WorkItemState>();
        _newState.CompletionSources.Add(ctx => completionSources);

        Options.Add(_workItemIdOption);
        Options.Add(_newState);
        Options.Add(_commentOption);
        Options.Add(_forOption);
    }


    protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
    {
        var witClient = _service.GetClient<WorkItemTrackingHttpClient>();

        // Resolve work item ID
        var workItemId =  parseResult.GetValue(_workItemIdOption);
        if (workItemId <= 0)
        {
            AnsiConsole.MarkupLine($"A valid work item ID is required. Run {WorkItemReadCommand.CommandText} to get an overview of active workitems".WithErrorMarkup());
            return ExitCodes.ResourceNoFound;
        }

        string? forUser = parseResult.GetValue(_forOption);

        if (!string.IsNullOrWhiteSpace(forUser))
        {
            var targetUser = await _userService.GetOrSelectUserAsync(forUser);
            forUser = targetUser.Email;
        }

        var newState = parseResult.GetValue(_newState);
        var comment = parseResult.GetValue(_commentOption);

        var patch = new JsonPatchDocument();

        if (newState != default)
        {
            patch.Add(CreatePatchOperation(Operation.Add, "/fields/System.State", newState.ToString()));
        }
        if (!string.IsNullOrWhiteSpace(forUser))
        {
            patch.Add(CreatePatchOperation(Operation.Add, "/fields/System.AssignedTo", forUser));
        }
        if (!string.IsNullOrWhiteSpace(comment))
        {
            patch.Add(CreatePatchOperation(Operation.Add, "/fields/System.History", comment));
        }

        if (patch.Count > 0)
        {
            var updatedWorkItem = await witClient.UpdateWorkItemAsync(patch, workItemId);
            AnsiConsole.MarkupLine($"Work item {updatedWorkItem.Id} updated successfully.".WithSuccessMarkup());
        }
        else
        {
            AnsiConsole.MarkupLine("No changes specified to update the work item.".WithWarningMarkup());
        }

        return ExitCodes.Ok;
    }

    public JsonPatchOperation CreatePatchOperation(Operation operation, string path, string newValue)
    {
        return new JsonPatchOperation
        {
            Operation = operation,
            Path = path,
            Value = newValue
        };
    }
}
