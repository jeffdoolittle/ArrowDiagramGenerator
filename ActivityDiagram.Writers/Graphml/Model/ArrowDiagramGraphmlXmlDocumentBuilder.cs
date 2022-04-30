namespace ActivityDiagram.Writers.Graphml.Model;

internal class ArrowDiagramGraphmlXmlDocumentBuilder
{
    private readonly List<graphmlGraphEdge> _edges;
    private readonly List<graphmlGraphNode> _nodes;

    public ArrowDiagramGraphmlXmlDocumentBuilder()
    {
        _edges = new List<graphmlGraphEdge>();
        _nodes = new List<graphmlGraphNode>();
    }

    public void AddNode(int id, GraphmlNodeType type, string label = null)
    {
        var nodeId = FormatNodeId(id);

        switch (type)
        {
            case GraphmlNodeType.Normal:
                _nodes.Add(GraphmlNodeBuilder.BuildNormal(nodeId));
                break;
            case GraphmlNodeType.Milestone:
                _nodes.Add(GraphmlNodeBuilder.BuildMilestone(nodeId, label));
                break;
            case GraphmlNodeType.GraphEnd:
            case GraphmlNodeType.GraphStart:
                _nodes.Add(GraphmlNodeBuilder.BuildTerminator(nodeId));
                break;
            default:
                break;
        }
    }

    public void AddEdge(int id, int sourceNodeId, int targetNodeId, GraphmlEdgeType type, string label = null)
    {
        var edgeId = FormatEdgeId(id);
        var stringSourceNodeId = FormatNodeId(sourceNodeId);
        var stringTargetNodeId = FormatNodeId(targetNodeId);

        switch (type)
        {
            case GraphmlEdgeType.Activity:
                _edges.Add(GraphmlEdgeBuilder.BuildActivity(edgeId, stringSourceNodeId, stringTargetNodeId, label));
                break;
            case GraphmlEdgeType.CriticalActivity:
                _edges.Add(GraphmlEdgeBuilder.BuildCriticalActivity(edgeId, stringSourceNodeId, stringTargetNodeId, label));
                break;
            case GraphmlEdgeType.Dummy:
                _edges.Add(GraphmlEdgeBuilder.BuildDummy(edgeId, stringSourceNodeId, stringTargetNodeId));
                break;
            case GraphmlEdgeType.CriticalDummy:
                _edges.Add(GraphmlEdgeBuilder.BuildCriticalDummy(edgeId, stringSourceNodeId, stringTargetNodeId));
                break;
            default:
                break;
        }

    }

    private string FormatNodeId(int id) => string.Format("n{0}", id);

    private string FormatEdgeId(int id) => string.Format("e{0}", id);

    public graphml Build()
    {
        var graph = new graphmlGraph
        {
            id = "G",
            edgedefault = "directed",
            node = _nodes.ToArray(),
            edge = _edges.ToArray()
        };

        return BuildGraphmlInternal(graph);
    }

    private graphml BuildGraphmlInternal(graphmlGraph graph) => new()
    {
        Items = new object[]
            {
                new graphmlKey() { @for = "node", id = "d6", yfilestype = "nodegraphics" },
                new graphmlKey() { @for = "edge", id = "d10", yfilestype = "edgegraphics" },
                graph
            }
    };
}
