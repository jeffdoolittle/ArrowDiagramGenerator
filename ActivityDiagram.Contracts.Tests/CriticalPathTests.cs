using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ActivityDiagram.Contracts.Tests;

#pragma warning disable IDE0058 // discard not required in tests

public class CriticalPathTests
{
    [Fact]
    public void Go()
    {
        var services = new ServiceCollection()
            .AddLogging(logging => logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug))
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<CriticalPathTests>>();

        var activities = new List<Activity>();

        var start = new Activity
        {
            Id = 1,
            Duration = 0,
            Predecessors = { },
            Successors = { 2, 3 }
        };
        var req = new Activity
        {
            Id = 2,
            Duration = 15,
            Predecessors = { 1 },
            Successors = { 4 }
        };
        var arch = new Activity
        {
            Id = 3,
            Duration = 15,
            Predecessors = { 1 },
            Successors = { 4 }
        };
        var projectPlanning = new Activity
        {
            Id = 4,
            Duration = 10,
            Predecessors = { 2, 3 },
            Successors = { 5 }
        };
        var managementEducation = new Activity
        {
            Id = 5,
            Duration = 5,
            Predecessors = { 4 },
            Successors = { 6 }
        };
        var sdpReview = new Activity
        {
            Id = 6,
            Duration = 0,
            Predecessors = { 2, 5 },
            Successors = { 8 }
        };
        var devTraining = new Activity
        {
            Id = 8,
            Duration = 5,
            Predecessors = { 6 },
            // Successors = { 100 }
        };
        // var end = new Activity
        // {
        //     Id = 100,
        //     Duration = 0,
        //     Predecessors = { 8 },
        //     Successors = { }
        // };

        activities.AddRange(new[]
        {
            start,
            req,
            arch,
            projectPlanning,
            managementEducation,
            sdpReview,
            // devTraining,
            // end
        });

        activities.SetDurations(
                x => activities.Where(a => a.Successors.Contains(x.Id)),
                x => x.Duration,
                x =>
                {
                    x.Item.EarliestStart = x.DurationInfo.EarliestStart;
                    x.Item.LatestStart = x.DurationInfo.LatestStart;
                    x.Item.EarliestFinish = x.DurationInfo.EarliestFinish;
                    x.Item.LatestFinish = x.DurationInfo.LatestFinish;
                });

        start.EarliestStart.Should().Be(0);
        start.LatestStart.Should().Be(0);
        start.EarliestFinish.Should().Be(0);
        start.EarliestFinish.Should().Be(0);

        req.EarliestStart.Should().Be(0);
        req.LatestStart.Should().Be(0);
        req.EarliestFinish.Should().Be(15);
        req.LatestFinish.Should().Be(15);

        arch.EarliestStart.Should().Be(0);
        arch.LatestStart.Should().Be(0);
        arch.EarliestFinish.Should().Be(15);
        arch.LatestFinish.Should().Be(15);

        projectPlanning.EarliestStart.Should().Be(15);
        projectPlanning.LatestStart.Should().Be(15);
        projectPlanning.EarliestFinish.Should().Be(25);
        projectPlanning.LatestFinish.Should().Be(25);

        managementEducation.EarliestStart.Should().Be(25);
        managementEducation.LatestStart.Should().Be(25);
        managementEducation.EarliestFinish.Should().Be(30);
        managementEducation.LatestFinish.Should().Be(30);

        sdpReview.EarliestStart.Should().Be(30);
        sdpReview.LatestStart.Should().Be(30);
        sdpReview.EarliestFinish.Should().Be(30);
        sdpReview.LatestFinish.Should().Be(30);
    }

#pragma warning restore IDE0058 // discard not required in tests

    public class Activity
    {
        public List<int> Predecessors { get; init; } = new();

        public List<int> Successors { get; init; } = new();

        public int Id { get; init; }

        public int Duration { get; init; }

        public int EarliestStart { get; set; }

        public int LatestStart { get; set; }

        public int EarliestFinish { get; set; }

        public int LatestFinish { get; set; }

        public override string ToString() => $"{Id} ({Duration})";
    }

    public class Durationinfo<T>
    {
        public IEnumerable<T> Predecessors { get; private set; }
        public IEnumerable<T> Successors { get; private set; }

        public int EarliestStart { get; set; }

        public int LatestStart { get; set; }

        public int EarliestFinish { get; set; }

        public int LatestFinish { get; set; }

        public Durationinfo(IEnumerable<T> predecessors, IEnumerable<T> successors)
        {
            Predecessors = predecessors;
            Successors = successors;
        }

        public override string ToString() =>
            $"({EarliestStart} | {LatestStart}) -> ({EarliestFinish} | {LatestFinish})";
    }
}
