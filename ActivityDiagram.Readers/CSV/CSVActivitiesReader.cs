using System.Globalization;
using ActivityDiagram.Contracts;
using ActivityDiagram.Contracts.Model.Activities;
using ActivityDiagram.Readers.CSV.Model;
using CsvHelper;
using CsvHelper.Configuration;

namespace ActivityDiagram.Readers.CSV;

public class CSVActivitiesReader : IActivitiesReader, IDisposable
{
    private readonly CsvParser _csvParser;
    private readonly CsvReader _csvReader;

    public CSVActivitiesReader(string filename)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        _csvParser = new CsvParser(new StreamReader(filename), config);
        _csvReader = new CsvReader(_csvParser);
        _ = _csvReader.Context.RegisterClassMap<ActivityRowMap>();
    }

    public IEnumerable<ActivityDependency> Read()
    {
        var rows = _csvReader.GetRecords<ActivityRow>().ToList();

        var successors = new Dictionary<int, List<int>>();

        foreach(var id in rows.Select(r => r.ActivityId))
        {
            var matches = rows.Where(x => x.Predecessors.Contains(id));
            successors.Add(id, matches.Select(x => x.ActivityId).ToList());
        }

        var activityDependencies = rows.Select(actrow =>
            new ActivityDependency(
                new Activity(actrow.ActivityId, actrow.ActivityDuration, actrow.ActivityTotalSlack, actrow.ActivityName),
                actrow.Predecessors, successors[actrow.ActivityId])).ToList();

        return activityDependencies;
    }

    public void Dispose()
    {
        if (_csvReader != null)
        {
            _csvReader.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}

internal sealed class ActivityRowMap : ClassMap<ActivityRow>
{
    public ActivityRowMap()
    {
        _ = Map(m => m.ActivityId).Name("ID");
        _ = Map(m => m.Predecessors).Convert(ParsePredecessorsIntList);
        _ = Map(m => m.ActivityDuration).Convert(ParseDuration);
        _ = Map(m => m.ActivityTotalSlack).Convert(ParseTotalSlack);
        _ = Map(m => m.ActivityName).Name("Name");
    }

    private List<int> ParsePredecessorsIntList(ConvertFromStringArgs args) => ParseIntList(args.Row, "Predecessors");

    private int? ParseDuration(ConvertFromStringArgs args) => ParseSafeIntegerValuesWithSuffix(args.Row, "Duration", "days");

    private int? ParseTotalSlack(ConvertFromStringArgs args) => ParseSafeIntegerValuesWithSuffix(args.Row, "Total_Slack", "days");

    private static List<int> ParseIntList(IReaderRow row, string fieldName)
    {
        var stringList = row.GetField<string>(fieldName).Trim();
        if (string.IsNullOrEmpty(stringList))
        {
            return new List<int>();
        }

        return stringList.Split(',')
            .Select(sId => int.Parse(sId))
            .ToList();
    }

    private static int? ParseSafeIntegerValuesWithSuffix(IReaderRow row, string fieldName, string suffix)
    {
        var stringValue = row.GetField<string>(fieldName).Trim();
        if (string.IsNullOrEmpty(stringValue))
        {
            stringValue = row.GetField<string>(fieldName).Trim().Replace('_', ' ');

            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
        }

        try
        {
            var startIndexOfSuffix = stringValue.ToLowerInvariant().IndexOf(suffix);
            if (startIndexOfSuffix > 0)
            {
                stringValue = stringValue[..startIndexOfSuffix].Trim();
            }
        }
        catch { }

        if (int.TryParse(stringValue, out var integerValue))
        {
            return integerValue;
        }
        else
        {
            return null;
        }
    }
}
