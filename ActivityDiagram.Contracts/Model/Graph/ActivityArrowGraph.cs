namespace ActivityDiagram.Contracts.Model.Graph;

public class ActivityArrowGraph
{
    private readonly Dictionary<ActivityEdge, ActivityEdge> _edges;
    private readonly HashSet<EventVertex> _vertices;

    public ActivityArrowGraph()
    {
        _edges = new Dictionary<ActivityEdge, ActivityEdge>();
        _vertices = new HashSet<EventVertex>();
    }

    public int EdgeCount => _edges.Count;

    public IEnumerable<ActivityEdge> Edges => _edges.Keys;

    public bool ContainsEdge(ActivityEdge edge) => _edges.ContainsKey(edge);

    public bool AddEdge(ActivityEdge edge)
    {
        if (ContainsEdge(edge))
        {
            return false;
        }

        _edges.Add(edge, edge);
        _ = _vertices.Add(edge.Source);
        _ = _vertices.Add(edge.Target);
        return true;
    }

    public int AddEdgeRange(IEnumerable<ActivityEdge> edges)
    {
        var count = 0;
        foreach (var edge in edges)
        {
            if (AddEdge(edge))
            {
                count++;
            }
        }

        return count;
    }

    public bool RemoveEdge(ActivityEdge edge)
    {
        if (_edges.Remove(edge))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Clear()
    {
        _edges.Clear();
        _vertices.Clear();
    }

    public IEnumerable<EventVertex> Vertices => _vertices;

    public int VertexCount => _vertices.Count;
}
