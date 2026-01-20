using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Quacklibs.AzureDevopsCli.Core.Types.DailyReport;
using Quacklibs.AzureDevopsCli.Services;

namespace Quacklibs.AzureDevopsCli.Commands.Daily
{
    /// <summary>
    /// WIP: Retrieve an overview of performed work. 
    /// </summary>
    internal class DailyCommand : BaseCommand
    {
        private const string sinceOption = "--since";

        public Option<string> For = new("--for")
        {
            Required = false,
            Description = "The emailaddress for which to generate an daily report"
        };

        public Option<string> Since = new(sinceOption, "-s")
        {
            Required = false,
            Description = "The number of days to include in the report untill now",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => "9d"
        };

        private readonly AzureDevopsService _azdevopsService;
        private readonly AzureDevopsUserService _azdevopsUserService;

        public DailyCommand(AzureDevopsService azdevopsService, AzureDevopsUserService azdevopsUserService) : base("daily", "Overview of the work done of the last X days")
        {
            Options.Add(For);
            Options.Add(Since);

            _azdevopsService = azdevopsService;
            _azdevopsUserService = azdevopsUserService;
            For.DefaultValueFactory = _ => Settings.UserEmail;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult context)
        {
            string? generateReportForUser = context.GetValue(For);
            string? sinceValue = context.GetValue(Since);

            var projects = await _azdevopsService.GetClient<ProjectHttpClient>()
                                                 .GetProjects(stateFilter: ProjectState.WellFormed);

            var targetUser = await _azdevopsUserService.GetOrSelectUserAsync(generateReportForUser);

            if(targetUser is NoAzureDevopsUserFound usr)
            {
                AnsiConsole.WriteLine("No user found or selected");
                return ExitCodes.ResourceNoFound;
            }

            AnsiConsole.WriteLine($"\n using {targetUser.Email} \n");
            var dailyReportTimeRange = ParseDailyReportTimeRange(sinceValue);

            var dailyReport = new DailyReport(dailyReportTimeRange.from, dailyReportTimeRange.till, targetUser.Email);
            await AnsiConsole.Status().Spinner(Spinner.Known.Ascii).StartAsync($"Querying {projects.Count} projects", async ctx =>
            {

                ctx.Status = "Querying workitems";
                //TODO: workitem can be queried org wide. commits cannot
                var allWorkItems = await GetChangedWorkItems(dailyReportTimeRange.from, dailyReportTimeRange.till, targetUser.Email);
                var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 5 };

                ctx.Status = $"Querying commits using {parallelOptions.MaxDegreeOfParallelism} threads";

                await Parallel.ForEachAsync(projects, parallelOptions, async (project, _) =>
                 {
                     var commits = await GetCommits(project, targetUser.Email, dailyReportTimeRange.from, dailyReportTimeRange.till);
                     var projectWorkItems = allWorkItems.Where(e => e.Project == project.Name);

                     ctx.Status = $"{project.Name} finished";

                     var dailyReportEntry = new DailyProjectEntry(project.Name, projectWorkItems, commits);

                     dailyReport.AddEntry(dailyReportEntry);
                 });
            });

            dailyReport.GenerateReport();

            return ExitCodes.Ok;
        }

        public async Task<ProjectWorkItemChanges> GetChangedWorkItems(DateTime from, DateTime till, string forEmail)
        {
            // Work Items changed by the user
            string wiqlQuery = $"""
                               SELECT [System.Id], [System.Title], [System.ChangedDate]
                               FROM WorkItems
                               WHERE [System.ChangedBy] = '{forEmail}'
                               AND [System.ChangedDate] >= '{from:yyyy-MM-dd}T00:00:00Z'
                               AND [System.ChangedDate] < '{till:yyyy-MM-dd}T00:00:00Z'
                               """;

            var client = _azdevopsService.GetClient<WorkItemTrackingHttpClient>();

            var wiql = new Wiql() { Query = wiqlQuery };
            var result = await client.QueryByWiqlAsync(wiql);

            var workitemChanges = new ProjectWorkItemChanges();
            foreach (var changedWorkItemsForThisProject in result.WorkItems)
            {
                //get parent info
                var workItem = await client.GetWorkItemAsync(changedWorkItemsForThisProject.Id, expand: WorkItemExpand.Relations);

                var projectName = workItem.Fields["System.TeamProject"].ToString();

                var parentRelation = workItem.Relations?.FirstOrDefault(r => r.Rel == "System.LinkTypes.Hierarchy-Reverse");
                // The relation's URL looks like: "https://dev.azure.com/{org}/{project}/_apis/wit/workItems/{id}"

                var hasParentWorkItemId = int.TryParse(parentRelation?.Url?.Split('/')?.Last(), out int parentWorkItemId);
                // Get all revisions of the work item
                var revisions = await client.GetRevisionsAsync(changedWorkItemsForThisProject.Id, expand: WorkItemExpand.All);

                var parentTitle = "N/A";

                if (hasParentWorkItemId)
                {
                    var parentWorkItem = await client.GetWorkItemAsync(parentWorkItemId, expand: WorkItemExpand.Fields);
                    parentTitle = parentWorkItem.Fields["System.Title"].ToString() ?? "N/A";
                }

                var workItemTitle = workItem.Fields["System.Title"].ToString() ?? "N/A";

                var projectWorkItemChange = new ProjectWorkItemChange(workItemTitle, parentTitle, parentWorkItemId, projectName ?? "No project name", changedWorkItemsForThisProject.Id);

                var comments = await client.GetCommentsAsync(changedWorkItemsForThisProject.Id);

                // Find comments added on target date
                if (comments?.Comments != null)
                {
                    foreach (var comment in comments.Comments)
                    {
                        var commentedDate = comment.RevisedDate;
                        if (commentedDate.Date >= from && commentedDate < till.AddDays(1))
                        {
                            projectWorkItemChange.AddChange(new WorkItemCommentChanged(commentedDate, comment.Text, comment.RevisedBy.DisplayName));
                        }
                    }
                }

                string? previousState = null;

                // Find the revision(s) changed on target date
                var orderedRevisions = revisions.OrderBy(r => (DateTime)r.Fields["System.ChangedDate"]);
                foreach (var rev in orderedRevisions)
                {
                    var changedDate = (DateTime)rev.Fields["System.ChangedDate"];

                    if (changedDate.Date >= from && changedDate < till.AddDays(1))
                    {
                        var currentState = rev.Fields["System.State"]?.ToString();

                        bool isCreated = rev == revisions.First();

                        if (isCreated)
                            projectWorkItemChange.AddChange(new WorkItemCreated(changedDate, currentState));

                        if (previousState == null)
                            previousState = currentState;

                        if (currentState != previousState)
                        {
                            projectWorkItemChange.AddChange(new WorkItemStateChanged(changedDate, previousState, currentState));
                            previousState = currentState;
                        }
                    }
                }

                workitemChanges.Add(projectWorkItemChange);
            }

            return workitemChanges;
        }

        public async Task<ProjectCommitChanges> GetCommits(TeamProjectReference project, string authorEmail, DateTime from, DateTime till)
        {
            // Get all repositories in the project
            var client = _azdevopsService.GetClient<GitHttpClient>();
            var repos = await client.GetRepositoriesAsync(project.Name);

            var commitChanges = new ProjectCommitChanges();

            foreach (var repo in repos)
            {
                var commits = await client.GetCommitsAsync(project.Name, repo.Id, searchCriteria: new GitQueryCommitsCriteria
                {
                    Author = authorEmail,
                    FromDate = from.ToString("o"),
                    ToDate = till.ToString("o")
                });

                var changesInCurrentRepo = commits.Select(e => new CommitChange(e.CommitId, e.Author.Name, e.Comment, e.Url, e.Author.Date));

                if (changesInCurrentRepo.Any())
                {
                    commitChanges.AddRange(changesInCurrentRepo);
                }
            }

            return commitChanges;
        }

        private (DateTime from, DateTime till) ParseDailyReportTimeRange(string? userInputForSinceParameter)
        {
            string errorText = $"The since parameter '{userInputForSinceParameter}' is invalid. Please provide a valid number followed by 'd' for days or 'w' for weeks. Example: {sinceOption} 7d or {sinceOption} 2w";

            if (string.IsNullOrEmpty(userInputForSinceParameter))
                throw new ArgumentException(errorText);

            // Everything before the unit should be the number
            bool hasValidAmountIdentifier = int.TryParse(userInputForSinceParameter[..^1], out int amountIdentifier);
            string timeRangeIdentifier = userInputForSinceParameter[^1].ToString().ToLowerInvariant();

            if (!hasValidAmountIdentifier)
                throw new ArgumentException(errorText);

            var daysInPast = timeRangeIdentifier switch
            {
                "d" => amountIdentifier,
                "w" => amountIdentifier * 7,
                _ => amountIdentifier //invalid or no input? default to days
            };

            var from = DateTime.Today.AddDays(daysInPast * -1);
            var to = DateTime.Now;

            return (from, to);
        }
    }
}