using Microsoft.TeamFoundation.SourceControl.WebApi;
using Quacklibs.AzureDevopsCli.Services;
using System.Collections.Concurrent;

namespace Quacklibs.AzureDevopsCli.Commands.PullRequests
{
    internal class PullRequestReadCommand : BaseCommand
    {
        private readonly AzureDevopsService _service;
        private readonly AzureDevopsUserService _azdevopsUserService;

        private readonly Option<string> _forOption = new(CommandOptionConstants.ForOptionName);

        public PullRequestReadCommand(AzureDevopsService service, AzureDevopsUserService azdevopsUserService) : base(CommandConstants.ReadCommand, "Read pull requests for the current user")
        {
            _service = service;
            _azdevopsUserService = azdevopsUserService;

            Options.Add(_forOption);
            _forOption.DefaultValueFactory = _ => Settings.UserEmail;
        }

        protected override async Task<int> OnExecuteAsync(ParseResult parseResult)
        {
            var gitClient = _service.GetClient<GitHttpClient>();

            var user = parseResult.GetValue(_forOption);

            //TODO: Test if the ID returned here is the correct one, or if the locationHTTPClient needs to be used to resolve the descriptor to an ID
            var targetUser = await _azdevopsUserService.GetOrSelectUserAsync(user);
            Console.WriteLine($"Querying for {targetUser.Email}");
            var identityGuid = targetUser.Id;

            if (targetUser is NoAzureDevopsUserFound)
            {
                AnsiConsole.WriteLine("No user found");
                return ExitCodes.ResourceNoFound;
            }

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
                        CreatorId = identityGuid,
                    }, cancellationToken: cancellationToken);

                    // Get PRs where current user is a reviewer
                    var reviewerPRs = await gitClient.GetPullRequestsAsync(repo.Id, new GitPullRequestSearchCriteria
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
                        .WithRows(allRelevantPrs)
                        .Build();

            AnsiConsole.Write(table);


            AnsiConsole.WriteLine($"run {PullRequestOpenCommand.SampleCommand} to open PR");
            return ExitCodes.Ok;
        }
    }
}