using System.Collections;

namespace Quacklibs.AzureDevopsCli.Core.Types.DailyReport
{
    public interface IPullRequestChange;
    public record PullRequestCreated(DateTime CreatedOn, string branch) : IPullRequestChange;
    public record PullRequestReviewed(DateTime ReviewedOn, string ReviewedBy, string branch) : IPullRequestChange;
    public record PullRequestClosed(DateTime ReviewedOn, string ReviewedBy, string branch) : IPullRequestChange;

    public record ProjectPullRequestChanges(string ProjectName) : IEnumerable<IPullRequestChange>
    {
        private List<IPullRequestChange> _internalProjectPullRequestChanges = [];

        public void Add(IPullRequestChange prChange) => _internalProjectPullRequestChanges.Add(prChange);

        public IEnumerator<IPullRequestChange> GetEnumerator() => _internalProjectPullRequestChanges.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
