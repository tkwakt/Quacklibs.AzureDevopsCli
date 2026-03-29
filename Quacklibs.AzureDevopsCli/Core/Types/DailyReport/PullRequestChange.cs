using System.Collections;

namespace Quacklibs.AzureDevopsCli.Core.Types.DailyReport
{
    public interface IPullRequestChange
    {
        public string GetDateDescription { get; }
        public string GetStatus { get; }
        public string GetDescription { get; }
    }

    public record PullRequestReviewed(DateTime ReviewedOn, string ReviewedBy, string branch) : IPullRequestChange
    {
        public string GetDateDescription => ReviewedOn.ToShortDateString();

        public string GetStatus => "Reviewed";

        public string GetDescription => $"Reviewed by {ReviewedBy}";
    }

    public record PullRequestClosed(int pullRequestId, DateTime ReviewedOn, string Reviewers, string Description, string state) : IPullRequestChange
    {
        public string GetDateDescription => ReviewedOn.ToShortDateString();

        public string GetStatus => state;

        public string GetDescription => $"{pullRequestId} - {Reviewers} - {Description}"; 
    }


    public record PullRequestActive(int Id, DateTime ReviewedOn, string Description) : IPullRequestChange
    {
        public string GetDateDescription => ReviewedOn.ToShortDateString();

        public string GetStatus => "Active";

        public string GetDescription => Description;
    }


    public record ProjectPullRequestChanges(string ProjectName) : IEnumerable<IPullRequestChange>
    {
        private List<IPullRequestChange> _internalProjectPullRequestChanges = [];

        public void Add(IPullRequestChange prChange) => _internalProjectPullRequestChanges.Add(prChange);

        public IEnumerator<IPullRequestChange> GetEnumerator() => _internalProjectPullRequestChanges.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
