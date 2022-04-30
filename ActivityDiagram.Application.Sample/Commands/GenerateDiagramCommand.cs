using ActivityDiagram.Contracts;
using ActivityDiagram.Generator;
using ActivityDiagram.Readers.CSV;
using ActivityDiagram.Readers.Mpp;
using ActivityDiagram.Writers.Graphml;
using ActivityDiagram.Writers.Graphviz;
using ManyConsole;

namespace ActivityDiagram.Application.Sample.Commands;

internal class GenerateDiagramCommand : ConsoleCommand
{
    public GenerateDiagramCommand()
    {
        _ = IsCommand("gen", "Generates an arrow diagram from an activity dependency graph.");

        _ = HasOption("it|intype=", "The file type of the input activity dependencies file. Available types: csv, mpp. default: csv", s => _inputType = s ?? "csv");
        _ = HasOption("ot|outtype=", "The file type of the output arrow diagram. Available types: graphml, dot. default: graphml", s => _outputType = s ?? "graphml");
        _ = HasOption("o|output=", "The output file name. default: '<intput file>.out.type'", s => _outputFile = s ?? "");

        _ = HasAdditionalArguments(1, "<input file>");
    }

    private string _inputType = "csv";
    private string _inputFile;
    private string _outputType = "graphml";
    private string _outputFile;
    public override int Run(string[] remainingArguments)
    {
        CheckRequiredArguments();
        _inputType = _inputType.ToLower();
        _outputType = _outputType.ToLower();

        _inputFile = remainingArguments[0];
        if (string.IsNullOrEmpty(_inputFile))
        {
            throw new ConsoleHelpAsException(string.Format("The input file name '{0}' is not valid", _inputFile));
        }


        if (string.IsNullOrEmpty(_outputFile))
        {
            _outputFile = _inputFile + ".out." + _outputType;
        }


        var reader = GetReaderForType(_inputType);
        Console.WriteLine("Using activities input file {0}", _inputFile);

        var writer = GetWriterForType(_outputType, _outputFile);
        Console.WriteLine("Using arrow diagram output file {0}", _outputFile);

        if (writer != null && reader != null)
        {
            try
            {
                CreateArrowDiagram(reader, writer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to generate the diagram. Exception:\n{0}", ex.ToString());
                return -1;
            }
        }

        return 0;
    }

    private static void CreateArrowDiagram(IActivitiesReader reader, IArrowGraphWriter writer)
    {
        Console.WriteLine("Reading activities...");
        var activities = reader.Read();
        Console.WriteLine("Calculating critical path and durations...");
        activities.SetDurations(activity => activity.Predecessors
                .SelectMany(p => activities.Where(a => a.Activity.Id == p)),
                a => a.Activity.Duration ?? 0,
                x =>
                {
                    x.Item.EarliestStart = x.DurationInfo.EarliestStart;
                    x.Item.LatestStart = x.DurationInfo.LatestStart;
                    x.Item.EarliestFinish = x.DurationInfo.EarliestFinish;
                    x.Item.LatestFinish = x.DurationInfo.LatestFinish;
                });
        Console.WriteLine("Generating Graph...");
        var graphGenerator = new ArrowGraphGenerator(activities);
        var arrowGraph = graphGenerator.GenerateGraph();
        Console.WriteLine("Writing Graph...");
        writer.Write(arrowGraph);
        Console.WriteLine("Done.");
    }

    private IActivitiesReader GetReaderForType(string type)
    {
        try
        {
            return type switch
            {
                "csv" => GetCsvReader(_inputFile),
                "mpp" => GetMppReader(_inputFile),
                _ => throw new ConsoleHelpAsException(string.Format("The input type {0} is not supported", type)),
            };
        }
        catch (ConsoleHelpAsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to create reader. Exception:\n{0}", ex.ToString());
        }

        return null;
    }

    private static IActivitiesReader GetMppReader(string inputFile) => new MppActivitiesReader(inputFile);

    private static IActivitiesReader GetCsvReader(string inputFile) => new CSVActivitiesReader(inputFile);

    private IArrowGraphWriter GetWriterForType(string type, string outputFile)
    {
        try
        {
            return type switch
            {
                "graphml" => GetGraphMLWriter(outputFile),
                "dot" => GetGraphVizWriter(outputFile),
                _ => throw new ConsoleHelpAsException(string.Format("The output type {0} is not supported", _outputType)),
            };
        }
        catch (ConsoleHelpAsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unable to create writer. Exception:\n{0}", ex.ToString());
        }

        return null;
    }

    private static IArrowGraphWriter GetGraphVizWriter(string outputFile) => new GraphvizArrowGraphWriter(outputFile);

    private static IArrowGraphWriter GetGraphMLWriter(string outputFile) => new GraphmlArrowGraphWriter(outputFile);
}
