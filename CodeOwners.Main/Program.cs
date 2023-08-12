using CodeOwners.IO.CodeOwnerFinder;
using CodeOwners.IO.Git;
using CodeOwners.IO.Notifier;
using CodeOwners.IO.Parser;
using CodeOwners.IO.PullRequests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeOwners
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddLogging(loggerBuilder =>
                {
                    loggerBuilder.ClearProviders()
                        .AddFilter("*", LogLevel.Debug)
                        .AddConsole();
                })
                .AddSingleton(configuration)
                .AddSingleton<IWorker, Worker>()
                .AddSingleton<IGitHelper, GitHelper>()
                .AddSingleton<ICodeOwnersParser, CodeOwnersParser>()
                .AddSingleton<ICodeOwnersFinder, CodeOwnersFinder>()
                .AddSingleton<IPullRequestsDiscover, AdoPullRequestsDiscover>()
                .AddSingleton<ANotifier, RocketChatNotifier>()
                .AddHttpClient()
                .BuildServiceProvider();

            var logger = serviceProvider.GetService<ILoggerFactory>()?
                .CreateLogger<Program>();
            logger?.LogDebug("Starting application");

            //do the actual work here
            var worker = serviceProvider.GetService<IWorker>();
            worker?.RunAsync(CancellationToken.None).Wait();

            logger?.LogDebug("All done!");
        }
    }
}