using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Location.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Quacklibs.AzureDevopsCli.Core.Behavior;
using System.Collections.Concurrent;

namespace Quacklibs.AzureDevopsCli.Commands.PullRequests
{
    public class PullRequestReadCommand : BaseCommand
    {
        private readonly AzureDevopsService _service;

        private Option<string> For = new("--for"); //WIP /`/ TODO

        public PullRequestReadCommand(AzureDevopsService service) : base("read", "Read pull requests for the current user")
        {
            this.Options.Add(For);
            For.DefaultValueFactory = _ => Settings.UserEmail;
            _service = service;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var gitClient = _service.GetClient<GitHttpClient>();
            var identityClient = _service.GetClient<LocationHttpClient>();
            var userEmail = parseResult.GetValue(For);

            var identiesWithThisUserId = await identityClient.GetConnectionDataAsync(ConnectOptions.None, lastChangeId: -1);

            Console.WriteLine($"Querying for {identiesWithThisUserId.AuthenticatedUser.DisplayName}");
            var identity = identiesWithThisUserId.AuthenticatedUser.Id;


            List<GitRepository> repositories = await gitClient.GetRepositoriesAsync();
            var sanitizedRepos = repositories.Where(e => e?.IsDisabled is false)
                                             .Where(e => e?.IsInMaintenance is false)
                                             .ToList();


            var allRelevantPrs = new ConcurrentBag<GitPullRequest>();

            AnsiConsole.Write($"\n Polling {repositories.Count} non disabled, non in-maintanance repo's for pr's. this may take a while \n ");

            await AnsiConsole.Status().Spinner(Spinner.Known.Ascii).StartAsync($"Polling non disabled, non in-maintanance repo's for pr's. this may take a while \\n \");\r\n", async ctx =>
            {
                await Parallel.ForEachAsync(sanitizedRepos, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, async (repo, cancellationToken) =>
            {
                try
                {
                    // Get PRs where current user is the creator
                    var createdPRs = await gitClient.GetPullRequestsAsync(repo.Id, new GitPullRequestSearchCriteria
                    {
                        Status = PullRequestStatus.Active,
                        CreatorId = identiesWithThisUserId.AuthenticatedUser.Id
                    }, cancellationToken: cancellationToken);

                    // Get PRs where current user is a reviewer
                    var reviewerPRs = await gitClient.GetPullRequestsAsync(repo.Id, new GitPullRequestSearchCriteria { Status = PullRequestStatus.Active, ReviewerId = identiesWithThisUserId.AuthenticatedUser.Id }, cancellationToken: cancellationToken);

                    // Combine and deduplicate by PR ID
                    createdPRs.AddRange(reviewerPRs);
                    var combinedCollection = createdPRs.DistinctBy(e => e.PullRequestId);

                    foreach (var review in combinedCollection)
                    {
                        allRelevantPrs.Add(review);
                    }

                    ctx.Status = $"{repo.Name} processed";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"repo {repo.Name} failed: {ex.Message}");
                }
            });
            });

            if (!allRelevantPrs.Any())
            {
                Console.WriteLine("No pr's");
            }
            var table = TableBuilder<GitPullRequest>
                        .Create()
                        .WithTitle("Pull requests")
                        .WithColumn("Id", new(e => e.PullRequestId.ToString()))
                        .WithColumn("Title", new(e => e.Title))
                        .WithColumn("Date", new(e => e.CreationDate.ToString("dd-MM-yyyy")))
                        .WithColumn("Repo", new(e => e.Repository?.Name))
                        .WithColumn("Submitter", new(e => e.CreatedBy?.DisplayName))
                        .WithColumn("IsReviewed", new(e => e.Reviewers.Any(rv => rv.Vote >= 5) ? "true" : "false"))
                        .WithColumn("Link", new(e => e.RemoteUrl?.ToString()?.AsUrlMarkup()))
                        .WithRows(allRelevantPrs)
                        .Build();

            AnsiConsole.Write(table);

            return ExitCodes.Ok;
        }
    }
}