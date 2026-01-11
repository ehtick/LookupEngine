using Build.Modules;
using Build.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Extensions;
using ModularPipelines.Host;

await PipelineHostBuilder.Create()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, collection) =>
    {
        if (args.Length == 0)
        {
            collection.AddModule<CleanProjectModule>();
            collection.AddModule<CompileProjectModule>();
        }

        if (args.Contains("test"))
        {
            collection.AddModule<TestProjectModule>();
        }

        if (args.Contains("publish"))
        {
            collection.AddOptions<PublishOptions>().Bind(context.Configuration.GetSection("Publish")).ValidateDataAnnotations();

            collection.AddModule<ResolveBuildVersionModule>();
            collection.AddModule<GenerateChangelogModule>();
            collection.AddModule<GenerateGitHubChangelogModule>();
            collection.AddModule<PublishGithubModule>();
        }
    })
    .ExecutePipelineAsync();