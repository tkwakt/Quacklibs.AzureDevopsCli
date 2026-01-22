using Quacklibs.AzureDevopsCli.Core.Behavior;
using System.Collections;

namespace Quacklibs.AzureDevopsCli.Core.Types.DailyReport;

public class ProjectWorkItemChange(string title, string parentTitle, int parentWorkItemId, string project, int Id)
{
    public readonly List<WorkItemChange> Changes = [];
    public string Title { get; } = title;

    public int ParentWorkItemId { get; } = parentWorkItemId;
    public string ParentTitle { get; } = parentTitle;
    public string Project { get; } = project;
    public int Id { get; } = Id;

    public void AddChange(WorkItemChange change)
    {
        Changes.Add(change);
    }

    public void AddChanges(IEnumerable<WorkItemChange> changes) => changes.ToList().ForEach(AddChange);
    public bool Any() => Changes.Count > 0;
}


public class ProjectWorkItemChanges : IEnumerable<ProjectWorkItemChange>
{
    private List<ProjectWorkItemChange> Changes = [];

    public ProjectWorkItemChanges() { }

    public void Add(ProjectWorkItemChange change)
    {
        Changes.Add(change);
    }

    public IEnumerator<ProjectWorkItemChange> GetEnumerator() => Changes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


public abstract class WorkItemChange
{
    public WorkItemChange()
    {
    }

    public abstract string DisplayText { get; }
    public abstract string DisplayType { get; }
}


public class WorkItemStateChanged : WorkItemChange
{

    public DateTime ChangeDate { get; set; }
    public string? FromState { get; }
    public string? ToState { get; }

    public WorkItemStateChanged(DateTime date, string? fromState, string? toState)
    {
        ChangeDate = date;
        FromState = fromState;
        ToState = toState;
    }

    public override string DisplayText => $"{ChangeDate.ToShortDateString()} {ChangeDate.ToShortTimeString()} From {FromState} to {ToState}";
    public override string DisplayType => "State Changed";
}

public class WorkItemCreated : WorkItemChange
{

    public DateTime CreatedDate { get; set; }
    public string State { get; }

    public WorkItemCreated(DateTime createdDate, string state)
    {
        CreatedDate = createdDate;
        State = state;
    }

    public override string DisplayText => $"{CreatedDate.ToShortDateString()} {CreatedDate.ToShortTimeString()}  Created in state {State}";
    public override string DisplayType => "Workitem Created";
}


public class WorkItemCommentChanged : WorkItemChange
{
    public DateTime ChangeDate { get; set; }
    public string CommentAuthor { get; }
    public HtmlContentType Comment { get; }

    public WorkItemCommentChanged(DateTime date, string comment, string commentAuthor)
    {
        ChangeDate = date;
        CommentAuthor = commentAuthor;
        Comment = new(comment.Trim());
    }

    public override string DisplayText => $"{ChangeDate.ToShortDateString()} {ChangeDate.ToShortTimeString()} by: {CommentAuthor}  {Comment.ToSpectreConsoleMarkup()} ";
    public override string DisplayType => "Comment added";
}