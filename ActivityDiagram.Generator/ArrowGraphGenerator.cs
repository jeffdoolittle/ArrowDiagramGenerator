using ActivityDiagram.Contracts.Model.Activities;
using ActivityDiagram.Contracts.Model.Graph;
using ActivityDiagram.Generator.Model;
using QuikGraph;
using QuikGraph.Algorithms;

namespace ActivityDiagram.Generator;

public class ArrowGraphGenerator
{
    private readonly IEnumerable<ActivityDependency> _activityDependencies;
    private readonly Dictionary<int, Activity> _activitiesDictionary;
    private BidirectionalGraph<InternalEventVertex, InternalActivityEdge> _arrowGraph;
    private Dictionary<Tuple<int, int>, int> _edgesIdsMap;
    private Dictionary<string, int> _verticeIdsMap;
    private int _edgesNextId = 0;
    private int _verticeNextId = 0;

    public ArrowGraphGenerator(IEnumerable<ActivityDependency> activityDependencies)
    {
        _activityDependencies = activityDependencies;
        _activitiesDictionary = _activityDependencies.ToDictionary(dep => dep.Activity.Id, dep => dep.Activity);
    }

    public ActivityArrowGraph GenerateGraph()
    {
        InitializeInternalStructures();

        var nodeGraph = CreateActivityNodeGraphFromProject();
        var nodeGraphReduction = nodeGraph.ComputeTransitiveReduction();
        _arrowGraph = ConvertActivityNodeGraphToArrowGraph(nodeGraphReduction);

        RedirectArrowGraph();
        DropRedundantArrows();

        foreach(var internalNode in _arrowGraph.Vertices)
        {
            var activity = _activityDependencies.Single(x => x.Activity.Id == internalNode.ActivityId);
            // internalNode.EarliestFinish = activity.EarliestFinish;
            // internalNode.LatestFinish = activity.LatestFinish;
        }

        var arrowGraph = CreateResultActivityArrowGraph();

        return arrowGraph;
    }

    private void InitializeInternalStructures()
    {
        _edgesIdsMap = new Dictionary<Tuple<int, int>, int>();
        _verticeIdsMap = new Dictionary<string, int>();
        _edgesNextId = 0;
        _verticeNextId = 0;
    }

    private BidirectionalGraph<int, SEdge<int>> CreateActivityNodeGraphFromProject() => _activityDependencies.
            SelectMany(act =>
                act.Predecessors.Select(pred =>
                    new SEdge<int>(
                        pred, // Source
                        act.Activity.Id // Target
                        ))).ToBidirectionalGraph<int, SEdge<int>>();

    private BidirectionalGraph<InternalEventVertex, InternalActivityEdge> ConvertActivityNodeGraphToArrowGraph(BidirectionalGraph<int, SEdge<int>> nodeGraph)
    {
        var arrowGraph = new BidirectionalGraph<InternalEventVertex, InternalActivityEdge>();

        // Go over all vertices - add them as new activity edges.
        foreach (var vertex in nodeGraph.Vertices)
        {
            var isCritical = _activitiesDictionary[vertex].IsCritical;

            var startNode = InternalEventVertex.Create(vertex, ActivityVertexType.ActivityStart, isCritical);
            var endNode = InternalEventVertex.Create(vertex, ActivityVertexType.ActivityEnd, isCritical);
            _ = arrowGraph.AddVertex(startNode);
            _ = arrowGraph.AddVertex(endNode);

            var activityEdge = new InternalActivityEdge(startNode, endNode, isCritical, vertex);

            _ = arrowGraph.AddEdge(activityEdge);
        }

        // Go over all edges - convert them to dummy edges.
        // Make sure connections are maintained.
        foreach (var edge in nodeGraph.Edges)
        {
            var source = _activitiesDictionary[edge.Source];
            var target = _activitiesDictionary[edge.Target];
            var isSourceCritical = source.IsCritical;
            var isTargetCritical = target.IsCritical;

            var activityEdge = new InternalActivityEdge(
                InternalEventVertex.Create(edge.Source, ActivityVertexType.ActivityEnd, isSourceCritical),
                InternalEventVertex.Create(edge.Target, ActivityVertexType.ActivityStart, isTargetCritical),
                isSourceCritical && isTargetCritical);

            _ = arrowGraph.AddEdge(activityEdge);
        }

        return arrowGraph;
    }

    /// <summary>
    /// This method implements the redirection phase.
    /// This phase looks at every activity end event and tries to pull as much dependency arrows off it's dependents
    /// to pass via it.
    /// This is a greedy process, where when ever a dependency is shared among all dependent nodes, it is redirected to point
    /// at the next vertex instead of on each one of them individually.
    /// </summary>
    private void RedirectArrowGraph()
    {
        // Go over every vertex
        foreach (var nexusVertex in _arrowGraph.Vertices)
        {
            // Nexus vertices are end events of activities
            if (nexusVertex.Type == ActivityVertexType.ActivityEnd)
            {
                // Get all the edges going out of the nexus
                if (_arrowGraph.TryGetOutEdges(nexusVertex, out var nexusOutEdges))
                {
                    var dependents = nexusOutEdges.Select(edge => edge.Target);
                    var commonDependencies = GetCommonDependenciesForNodeGroup(dependents);

                    // Aside from the obvious common dependency (the nexus vertex) redirect all dependencies to the nexus vertex
                    foreach (var commonDependency in commonDependencies.Where(d => d != nexusVertex))
                    {
                        RedirectCommonDependencyToNexus(nexusVertex, dependents, commonDependency);
                    }
                }
            }
        }
    }

    private HashSet<InternalEventVertex> GetCommonDependenciesForNodeGroup(IEnumerable<InternalEventVertex> vertices)
    {
        var commonDependencies = new HashSet<InternalEventVertex>();

        // Find the common dependencies for all target vertice
        foreach (var vertex in vertices)
        {
            if (_arrowGraph.TryGetInEdges(vertex, out var dependenciesOfTarget))
            {
                // Always work with dependencies which are dummies - since activities cannot/should not be redirected.
                dependenciesOfTarget = dependenciesOfTarget.Where(dep => !dep.ActivityId.HasValue);

                if (commonDependencies.Count == 0)
                {
                    foreach (var dependency in dependenciesOfTarget)
                    {
                        _ = commonDependencies.Add(dependency.Source);
                    }
                }
                else
                {
                    commonDependencies.IntersectWith(dependenciesOfTarget.Select(d => d.Source).AsEnumerable());
                }
            }
            // Else can never happen - the out edge for the current vertice is the in edge of the dependent
            // so at least once exists.
        }

        return commonDependencies;
    }

    private void RedirectCommonDependencyToNexus(InternalEventVertex nexusVertex, IEnumerable<InternalEventVertex> dependents, InternalEventVertex commonDependency)
    {
        var isAddedEdgeCritical = false;
        if (_arrowGraph.TryGetOutEdges(commonDependency, out var edgesOutOfDependency))
        {
            // Remove the edges between the dependency and the dependents of the nexus vertex
            var edgesToRemove = edgesOutOfDependency.Where(e => dependents.Contains(e.Target)).ToList();
            foreach (var edgeToRemove in edgesToRemove)
            {
                _ = _arrowGraph.RemoveEdge(edgeToRemove);

                // Replacing even one critical edge means the new edge would be also critical
                isAddedEdgeCritical = isAddedEdgeCritical || edgeToRemove.IsCritical;
            }
        }
        // Else should never happen

        // This dependency should point at nexus vertex
        var edgeToAdd = new InternalActivityEdge(commonDependency, nexusVertex, isAddedEdgeCritical);
        _ = _arrowGraph.AddEdge(edgeToAdd);
    }

    /// <summary>
    /// This methods implements the Drop phase which reduces the graph complexity by removing dummy edges which do not add new information.
    /// Those dummy edges connect a node which has only one input or only one output which is the dummy edge
    /// </summary>
    private void DropRedundantArrows()
    {
        var dummyEdgeRemovedOnIteration = true;

        while (dummyEdgeRemovedOnIteration)
        {
            // Get all the current dummy edges in the graph
            var nonActivityEdges = _arrowGraph.Edges.Where(e => !e.ActivityId.HasValue).ToList();

            foreach (var edge in nonActivityEdges)
            {
                // Only remove one edge at a time - then, need to reevaluate the graph.
                if (dummyEdgeRemovedOnIteration = TryRemoveDummyEdge(edge))
                {
                    break;
                }
            }
        }
    }

    private bool TryRemoveDummyEdge(InternalActivityEdge edge)
    {
        var edgeRemoved = false;

        // If this is a single edge out or a single edge in - it adds no information to the graph and can be merged.
        var outDegree = _arrowGraph.OutDegree(edge.Source);
        var inDegree = _arrowGraph.InDegree(edge.Target);

        // Remove the vertex which has no other edges connected to it
        if (outDegree == 1)
        {
            if (!_arrowGraph.TryGetInEdges(edge.Source, out var allIncoming))
            {
                allIncoming = new List<InternalActivityEdge>();
            }

            var abortMerge = WillParallelEdgesBeCreated(allIncoming, null, edge.Target);

            if (!abortMerge)
            {
                // Add the edges with the new source vertex
                // And remove the old edges
                foreach (var incomingEdge in allIncoming.ToList())
                {
                    _ = _arrowGraph.AddEdge(new InternalActivityEdge(incomingEdge.Source, edge.Target, incomingEdge.IsCritical, incomingEdge.ActivityId));
                    _ = _arrowGraph.RemoveEdge(incomingEdge);
                }

                // Remove the edge which is no longer needed
                _ = _arrowGraph.RemoveEdge(edge);

                // Now remove the vertex which is no longer needed
                _ = _arrowGraph.RemoveVertex(edge.Source);

                edgeRemoved = true;
            }
        }
        else if (inDegree == 1)
        {
            if (!_arrowGraph.TryGetOutEdges(edge.Target, out var allOutgoing))
            {
                allOutgoing = new List<InternalActivityEdge>();
            }

            var abortMerge = WillParallelEdgesBeCreated(allOutgoing, edge.Source, null);

            if (!abortMerge)
            {
                // Add the edges with the new source vertex
                // And remove the old edges
                foreach (var outgoingEdge in allOutgoing.ToList())
                {
                    _ = _arrowGraph.AddEdge(new InternalActivityEdge(edge.Source, outgoingEdge.Target, outgoingEdge.IsCritical, outgoingEdge.ActivityId));
                    _ = _arrowGraph.RemoveEdge(outgoingEdge);
                }

                // Remove the edge which is no longer needed
                _ = _arrowGraph.RemoveEdge(edge);

                // Now remove the vertex which is no longer needed
                _ = _arrowGraph.RemoveVertex(edge.Target);

                edgeRemoved = true;
            }
        }


        return edgeRemoved;
    }

    private bool WillParallelEdgesBeCreated(IEnumerable<InternalActivityEdge> plannedEdgesToReplace, InternalEventVertex plannedNewSource, InternalEventVertex plannedNewTarget)
    {
        var abortMerge = false;
        foreach (var edge in plannedEdgesToReplace.ToList())
        {
            var sourceToTestWith = plannedNewSource ?? edge.Source;
            var targetToTestWith = plannedNewTarget ?? edge.Target;

            if (_arrowGraph.TryGetEdge(sourceToTestWith, targetToTestWith, out var dummy))
            {
                abortMerge = abortMerge || true;
            }
        }

        return abortMerge;
    }

    private ActivityArrowGraph CreateResultActivityArrowGraph()
    {
        var activityArrowGraph = new ActivityArrowGraph();

        foreach (var edge in _arrowGraph.Edges)
        {
            var source = _activityDependencies.Single(x => x.Activity.Id == edge.Source.ActivityId);
            var target = _activityDependencies.Single(x => x.Activity.Id == edge.Target.ActivityId);

            // Console.WriteLine($"Edge Activity: {edge.ActivityId}");
            // Console.WriteLine($"Source Activity: {edge.Source.ActivityId}");
            // Console.WriteLine($"Target Activity: {edge.Target.ActivityId}");
            // Console.WriteLine();

            edge.Source.EarliestFinish = source.EarliestFinish;
            edge.Source.LatestFinish = source.LatestFinish;

            edge.Target.EarliestFinish = target.EarliestFinish;
            edge.Target.LatestFinish = target.LatestFinish;

            // if (edge.ActivityId.HasValue)
            // {
            //     var activity = _activitiesDictionary[edge.ActivityId.Value];
            //     edge.Source.EarliestFinish += activity.Duration;
            //     edge.Source.LatestFinish += activity.Duration;
            //     edge.Target.EarliestFinish += activity.Duration;
            //     edge.Target.LatestFinish += activity.Duration;
            // }

            var sourceVertex = CreateVertexEvent(edge.Source, _arrowGraph.InDegree(edge.Source), _arrowGraph.OutDegree(edge.Source));
            var targetVertex = CreateVertexEvent(edge.Target, _arrowGraph.InDegree(edge.Target), _arrowGraph.OutDegree(edge.Target));

            _ = TryGetActivity(edge, out var edgeActivity);

            _ = activityArrowGraph.AddEdge(CreateActivityEdge(sourceVertex, targetVertex, edgeActivity, edge.IsCritical));
        }

        return activityArrowGraph;
    }

    private ActivityEdge CreateActivityEdge(EventVertex source, EventVertex target, Activity edgeActivity, bool isCritical)
    {
        var edgeUniqueKey = new Tuple<int, int>(source.Id, target.Id);
        if (!_edgesIdsMap.TryGetValue(edgeUniqueKey, out var activityEdgeId))
        {
            _edgesIdsMap[edgeUniqueKey] = activityEdgeId = _edgesNextId;
            _edgesNextId++;
        }

        return new ActivityEdge(activityEdgeId, source, target, edgeActivity, isCritical);
    }

    private EventVertex CreateVertexEvent(InternalEventVertex vertex, int inDegree, int outDegree)
    {
        if (!_verticeIdsMap.TryGetValue(vertex.Id, out var eventVertexId))
        {
            _verticeIdsMap[vertex.Id] = eventVertexId = _verticeNextId;
            _verticeNextId++;
        }

        EventVertex eventVertex;
        if (inDegree == 0)
        {
            eventVertex = EventVertex.CreateGraphStart(eventVertexId, vertex);
        }
        else if (outDegree == 0)
        {
            eventVertex = EventVertex.CreateGraphEnd(eventVertexId, vertex);
        }
        else
        {
            eventVertex = EventVertex.Create(eventVertexId, vertex);
        }

        return eventVertex;
    }

    private bool TryGetActivity(InternalActivityEdge edge, out Activity activity)
    {
        activity = null;
        if (edge.ActivityId.HasValue && _activitiesDictionary.ContainsKey(edge.ActivityId.Value))
        {
            activity = _activitiesDictionary[edge.ActivityId.Value];
            return true;
        }

        return false;
    }

    private bool TryGetActivity(InternalEventVertex vertex, out Activity activity)
    {
        activity = null;
        return _activitiesDictionary.TryGetValue(vertex.ActivityId, out activity);
    }
}
