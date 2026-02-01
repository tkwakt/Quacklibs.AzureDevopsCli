using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Quacklibs.AzureDevopsCli.Core.Behavior.Console.Commandline;
using Quacklibs.AzureDevopsCli.Core.Types.DailyReport;
using Quacklibs.AzureDevopsCli.Services;
using System.Threading;

namespace Quacklibs.AzureDevopsCli.Commands.Daily
{
    /// <summary>
    /// Retrieve an overview of performed work. 
    /// </summary>
    internal class DailyCommand : BaseCommand
    {
        private readonly Option<string> _forOption = new(CommandOptionConstants.ForOptionName)
        {
            Required = false,
            Description = "Filter the report by person. The value can be an email address or (part of) a name. \r\n" +
                          "If multiple users match, an interactive selection is shown.\r\n" +
                          "The person is interpreted as the primary actor per type:\r\n" +
                          "  - Commits / pull requests: author \r\n" +
                          "  - Work items: changed by \r\n " 

        };

        private readonly Option<string> _sinceOption = new(CommandOptionConstants.SinceOptionName, CommandOptionConstants.SinceOptionAliasses)
        {
            Required = false,
            Description = "The number of days to include in the report untill now.",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => "lastworkday",
        };

        private readonly AzureDevopsService _azdevopsService;
        private readonly AzureDevopsUserService _azdevopsUserService;

        public DailyCommand(AzureDevopsService azdevopsService, AzureDevopsUserService azdevopsUserService) : base("daily", "Overview of the work done of the last X days")
        {
            Options.Add(_forOption);
            Options.Add(_sinceOption);

            _azdevopsService = azdevopsService;
            _azdevopsUserService = azdevopsUserService;

            _forOption.DefaultValueFactory = _ => Settings.UserEmail;
            _sinceOption.CompletionSources.Add(ctx => SinceParser.ToCompletionOptions());
        }

        protected override async Task<int> OnExecuteAsync(ParseResult context)
        {
            string? forUser = context.GetValue(_forOption);
            var sinceInput = new SinceType(context.GetValue(_sinceOption));

            var targetUser = await _azdevopsUserService.GetOrSelectUserAsync(forUser);

            if(targetUser is NoAzureDevopsUserFound usr)
            {
                AnsiConsole.MarkupLine("No user found or selected".WithWarningMarkup());
                return ExitCodes.ResourceNoFound;
            }

            AnsiConsole.WriteLine($"\n using {targetUser.Email} \n");

            var projects = await _azdevopsService.GetClient<ProjectHttpClient>()
                                                 .GetProjects(stateFilter: ProjectState.WellFormed);

            var dailyReportTimeRange = sinceInput.ToDateTimeRange();

            var dailyReport = new DailyReport(dailyReportTimeRange.from, dailyReportTimeRange.till, targetUser.Email);
            await AnsiConsole.Status().Spinner(Spinner.Known.Ascii).StartAsync($"Querying {projects.Count} projects", async ctx =>
            {
                ctx.Status = "Querying workitems";
                
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
            var fromUtc = from.ToString("yyyy-MM-dd");
            var tillUtc = till.ToString("yyyy-MM-dd");
            // Work Items changed by the user
            string wiqlQuery = $"""
                               SELECT [System.Id], [System.Title], [System.ChangedDate]
                               FROM WorkItems
                               WHERE [System.ChangedBy] = '{forEmail}'
                               AND [System.ChangedDate] >= '{fromUtc}'
                               AND [System.ChangedDate] <= '{tillUtc}'
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

        public async Task<ProjectPullRequestChanges> GetPullRequests(TeamProjectReference project, int authorId, DateTime from, DateTime till)
        {
            var gitClient = _azdevopsService.GetClient<GitHttpClient>();
            var repositoriesInProject = await gitClient.GetRepositoriesAsync(project.Name);



            foreach (var repositoryInProject in repositoriesInProject)
            {
                // Get PRs where current user is the creator
                var createdPRs = await gitClient.GetPullRequestsAsync(repositoriesInProject.Id, new GitPullRequestSearchCriteria
                {
                    Status = PullRequestStatus.Active,
                    CreatorId = identityGuid,
                }, cancellationToken: cancellationToken);

                // Get PRs where current user is a reviewer
                var reviewerPRs = await gitClient.GetPullRequestsAsync(repositoriesInProject.Id, new GitPullRequestSearchCriteria
                {
                    Status = PullRequestStatus.Active,
                    ReviewerId = identityGuid
                }, cancellationToken: cancellationToken);

                // Combine and deduplicate by PR ID
                createdPRs.AddRange(reviewerPRs);
                var combinedCollection = createdPRs.DistinctBy(e => e.PullRequestId);

                foreach (var review in combinedCollection)
                {
                    allRelevantPrs.Add(review);
                }

                ctx.Status = $"{repositoriesInProject.Name} processed";
            }
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"repo {repositoriesInProject.Name} failed: {ex.Message}");
            }
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
    }
}