namespace Quacklibs.AzureDevopsCli.Core.Types.Workitem
{
    public class WorkItemComment
    {
        public DateTime ChangeDate { get; set; }
        public string CommentAuthor { get; }
        public HtmlContentType Comment { get; }

        public WorkItemComment(DateTime date, string comment, string commentAuthor)
        {
            ChangeDate = date;
            CommentAuthor = commentAuthor;
            Comment = new(comment.Trim());
        }

        public string DisplayText => $"{ChangeDate.ToShortDateString()} {ChangeDate.ToShortTimeString()} by: {CommentAuthor} \n {Comment.ToSpectreConsoleMarkup()} ";
    }
}
