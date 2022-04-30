using System.Text;
using ActivityDiagram.Contracts;
using ActivityDiagram.Contracts.Model.Graph;

namespace ActivityDiagram.Writers.Graphviz;

public class GraphvizArrowGraphWriter : IArrowGraphWriter
{
    private readonly string _filename;
    public GraphvizArrowGraphWriter(string filename) => _filename = filename;

    private class Node
    {
        public int Id { get; set; }
    }

    public void Write(ActivityArrowGraph graph)
    {
        var sb = new StringBuilder();

        _ = sb.Append("digraph Arrow {\nrankdir=LR;\n");
        // change to oval once vertex calculations are fixed. see below.
        // _ = sb.Append("node [ shape=oval height=.3 width=.1 style=filled fillcolor=\"#e7e7e7\" fontsize=8 fontname=\"Sans-Serif\" penwidth=0 ]\n");
        _ = sb.Append("node [ shape=circle height=.4 width=.4 style=filled fillcolor=\"#e7e7e7\" fontsize=8 fontname=\"Sans-Serif\" penwidth=0 ]\n");
        foreach (var vertex in graph.Vertices)
        {
            // need to fis the vertex calculations
            // var node = $"N{vertex.Id} [ label=\"{vertex.EarliestFinish} | {vertex.LatestFinish}\" ];\n";
            var node = $"N{vertex.Id} [ label=\"\" ];\n";
            _ = sb.Append(node);
        }

        foreach (var edge in graph.Edges)
        {
            var penWidth = "1";

            if (edge.Activity != null)
            {
                var slack = edge.Activity.TotalSlack;
                var edgeColor = "darkgreen";
                var style = "solid";
                if (edge.Activity.Duration == 0)
                {
                    edgeColor = "black";
                    style = "dotted";
                }
                else
                {
                    if (slack < 1)
                    {
                        edgeColor = "black";
                        penWidth = "2";
                    }
                    else if (slack < 10)
                    {
                        edgeColor = "red";
                    }
                    else if (slack < 25)
                    {
                        edgeColor = "orange";
                    }
                }

                var tooltip = edge.Activity.Name;

                var labelBuilder = new StringBuilder();
                _ = labelBuilder.Append(edge.Activity.Id);
                if (edge.Activity.Duration > 0)
                {
                    _ = labelBuilder.Append($" ({edge.Activity.Duration})");
                    if (edge.Activity.TotalSlack > 0)
                    {
                        _ = labelBuilder.Append($"\n{edge.Activity.TotalSlack}");
                    }
                }

                var activity = $"N{edge.Source.Id} -> N{edge.Target.Id} [ id={edge.Activity.Id} style={style} edgetooltip=\"{tooltip}\" labeltooltip=\"{tooltip}\" penwidth=\"{penWidth}\" color=\"{edgeColor}\" fontsize=8 fontname=\"Sans-Serif\" label=\"{labelBuilder}\" ];\n";

                _ = sb.AppendFormat(activity);
            }
            else
            {
                _ = sb.AppendFormat("N{0} -> N{1} [ style=dashed penwidth=\"{2}\" fontsize=8 fontname=\"Sans-Serif\" ];\n", edge.Source.Id, edge.Target.Id, penWidth);
            }
        }
        _ = sb.Append('}');

        using var fileWriter = File.Create(_filename);
        var info = new UTF8Encoding(true).GetBytes(sb.ToString());
        fileWriter.Write(info, 0, info.Length);
    }
}
