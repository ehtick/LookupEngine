using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Git.Options;
using ModularPipelines.GitHub.Attributes;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Modules;
using Octokit;
using Status = ModularPipelines.Enums.Status;

namespace Build.Modules;

/// <summary>
///     Publish the templates to GitHub.
/// </summary>
[SkipIfNoGitHubToken]
[DependsOn<ResolveBuildVersionModule>]
[DependsOn<GenerateGitHubChangelogModule>]
public sealed class PublishGithubModule : Module<Release?>
{
    protected override async Task<Release?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var versioningResult = await GetModule<ResolveBuildVersionModule>();
        var changelogResult = await GetModule<GenerateGitHubChangelogModule>();

        var versioning = versioningResult.Value!;
        var changelog = changelogResult.Value!;

        var repositoryInfo = context.GitHub().RepositoryInfo;
        var newRelease = new NewRelease(versioning.Version)
        {
            Name = versioning.Version,
            Body = changelog,
            TargetCommitish = context.Git().Information.LastCommitSha,
            Prerelease = versioning.IsPrerelease
        };

        return await context.GitHub().Client.Repository.Release.Create(repositoryInfo.Owner, repositoryInfo.RepositoryName, newRelease);
    }

    protected override async Task OnAfterExecute(IPipelineContext context)
    {
        if (Status == Status.Failed)
        {
            var versioningResult = await GetModule<ResolveBuildVersionModule>();
            var versioning = versioningResult.Value!;

            await context.Git().Commands.Push(new GitPushOptions
            {
                Delete = true,
                Arguments = ["origin", versioning.Version]
            });
        }
    }
}