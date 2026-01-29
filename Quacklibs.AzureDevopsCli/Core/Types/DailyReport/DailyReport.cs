using System.Collections;

namespace Quacklibs.AzureDevopsCli.Core.Types.DailyReport
{
    public class DailyReport : IEnumerable<DailyProjectEntry>
    {
        private List<DailyProjectEntry> InternalDailyReportEntry = [];

        public DateTime From { get; }
        public DateTime To { get; }
        public string ForUser { get; }

        public DailyReport(DateTime from, DateTime to, string forUser)
        {
            From = from;
            To = to;
            ForUser = forUser;
        }

        public void AddEntry(DailyProjectEntry entry) => InternalDailyReportEntry.Add(entry);

        public void GenerateReport()
        {
            Console.WriteLine("\n");
            Console.WriteLine($"Daily Report for changes from {From:dd-MM-yyyy HH:mm} to {To:dd-MM-yyyy HH:mm} for {ForUser}");

            var projectsWithChanges = InternalDailyReportEntry.Where(re => re.hasChanges());

            if (!projectsWithChanges.Any())
            {
                AnsiConsole.MarkupLine("[green]No changes found![/]");
                return;
            }

            foreach (var dailyProjectEntry in projectsWithChanges)
            {
                AnsiConsole.WriteLine($"{dailyProjectEntry.Project}", new Style(decoration: Decoration.Bold));

                var groupedWorkItemChanges = dailyProjectEntry.WorkItemChanges.Where(e => e.Changes.Any())
                                                                              .GroupBy(e => new { e.ParentWorkItemId, e.ParentTitle });

                foreach (var parentWorkItem in groupedWorkItemChanges)
                {
                    var root = new Tree($"{parentWorkItem.Key.ParentWorkItemId} {parentWorkItem.Key.ParentTitle.EscapeMarkup()}").Guide(TreeGuide.Ascii);

                    foreach (var workItemChange in parentWorkItem)
                    {
                        var workItemNode = root.AddNode($"{workItemChange.Id} {workItemChange.Title.EscapeMarkup().Highlight()}");

                        foreach (var item in workItemChange.Changes)
                        {
                            workItemNode.AddNode($"{item.DisplayType.EscapeMarkup()} - {item.DisplayText.EscapeMarkup()}");
                        }
                    }

                    Console.WriteLine();
                    AnsiConsole.Write(root);
                }

                AnsiConsole.WriteLine();

                var cmmitTree = new Tree($"Commit");
                foreach (var commitChange in dailyProjectEntry.CommitChanges)
                {
                    cmmitTree.AddNode($"{commitChange.CreatedAt} Commit {commitChange.Author} {commitChange.Comment}");
                }

                if (cmmitTree.Nodes.Any())
                {
                    Console.WriteLine();
                    AnsiConsole.Write(cmmitTree);
                }
            }
        }

        public IEnumerator<DailyProjectEntry> GetEnumerator() => InternalDailyReportEntry.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => InternalDailyReportEntry.GetEnumerator();

    }

    public class DailyProjectEntry
    {
        public string Project { get; set; }
        public ProjectWorkItemChanges WorkItemChanges { get; } = [];
        public ProjectCommitChanges CommitChanges { get; private set; } = [];

        public DailyProjectEntry(string projectName, IEnumerable<ProjectWorkItemChange> projectWorkItems, ProjectCommitChanges projectCommitChanges)
        {
            Project = projectName;
            AddRange(projectWorkItems);
            CommitChanges = projectCommitChanges;
        }

        public void AddRange(IEnumerable<ProjectWorkItemChange> changes) => changes.ToList().ForEach(Add);

        public void Add(ProjectWorkItemChange change)
        {
            WorkItemChanges.Add(change);
        }

        public bool hasChanges() => WorkItemChanges.Any() || CommitChanges.Any();
    }


    public class ProjectCommitChanges : List<CommitChange>
    {

    }
}