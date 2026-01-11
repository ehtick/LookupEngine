using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using Sourcy.DotNet;

namespace Build.Modules;

/// <summary>
///     Compile the project.
/// </summary>
[DependsOn<CleanProjectModule>]
public sealed class CompileProjectModule : Module<CommandResult>
{
    protected override async Task<CommandResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await context.DotNet().Build(new DotNetBuildOptions
        {
            ProjectSolution = Projects.LookupEngine.FullName,
            Configuration = Configuration.Release,
            Verbosity = Verbosity.Minimal
        }, cancellationToken);
    }
}