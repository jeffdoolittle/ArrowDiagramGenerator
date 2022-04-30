using System.IO;
using System.Linq;
using System.Text.Json;
using ActivityDiagram.Contracts;
using ActivityDiagram.Readers.CSV;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ActivityDiagram.Generator.Tests;

public class ActivityTests
{
    [Fact]
    public void Test1()
    {
        var services = new ServiceCollection()
            .AddLogging(logging => logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug))
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<ActivityTests>>();

        var location = Path.GetDirectoryName(typeof(ActivityTests).Assembly.Location)!;
        var filename = Path.Combine(location, "activities.csv");

        var reader = new CSVActivitiesReader(filename);
        var activityDependencies = reader.Read();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        // var json = JsonSerializer.Serialize(activityDependencies, options);


        var criticalPath = activityDependencies
            .CriticalPath(activity => activity.Predecessors
                .SelectMany(p => activityDependencies.Where(a => a.Activity.Id == p)),
                a => a.Activity.Duration ?? 0);

        logger.LogInformation("Original activity count: {ActivityCount}", activityDependencies.Count());
        logger.LogInformation("Critical activity count: {CriticalCount}", criticalPath.Count());

        activityDependencies.SetDurations(activity => activity.Predecessors
                .SelectMany(p => activityDependencies.Where(a => a.Activity.Id == p)),
                a => a.Activity.Duration ?? 0,
                x =>
                {
                    x.Item.EarliestStart = x.DurationInfo.EarliestStart;
                    x.Item.LatestStart = x.DurationInfo.LatestStart;
                    x.Item.EarliestFinish = x.DurationInfo.EarliestFinish;
                    x.Item.LatestFinish = x.DurationInfo.LatestFinish;
                });

        // var activityDependenciesJson = JsonSerializer.Serialize(activityDependencies, options);
        // logger.LogInformation("{activityDependenciesJson}", activityDependenciesJson);

        var generator = new ArrowGraphGenerator(activityDependencies);
        var graph = generator.GenerateGraph();

        var graphJson = JsonSerializer.Serialize(activityDependencies, options);
        logger.LogInformation("{graphJson}", graphJson);

    }
}
