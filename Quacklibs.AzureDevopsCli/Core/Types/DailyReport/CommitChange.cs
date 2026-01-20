namespace Quacklibs.AzureDevopsCli.Core.Types.DailyReport;

public class CommitChange(string CommitId, string Author, string Comment, string Url, DateTime createdAt)
{
    public DateTime CreatedAt { get; set; } = createdAt;
    public string CommitId { get; } = CommitId;
    public string Author { get; } = Author;
    public string Comment { get; } = Comment;
    public string Url { get; } = Url;
}