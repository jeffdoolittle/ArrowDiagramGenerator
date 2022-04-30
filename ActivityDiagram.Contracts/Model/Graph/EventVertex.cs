using ActivityDiagram.Contracts.Model.Activities;

namespace ActivityDiagram.Contracts.Model.Graph;

public class EventVertex
{
    public int Id { get; private set; }
    public EventVertexType Type { get; private set; } = EventVertexType.Normal;
    public Activity MilestoneActivity { get; private set; } = null;
    public int? EarliestStart { get; set; }
    public int? LatestStart { get; set; }
    public int? EarliestFinish { get; set; }
    public int? LatestFinish { get; set; }

    private EventVertex(int id, EventVertexType vertexType, IDurationInfo durationInfo)
    {
        Id = id;
        Type = vertexType;
        EarliestStart = durationInfo?.EarliestStart;
        LatestStart = durationInfo?.LatestStart;
        EarliestFinish = durationInfo?.EarliestFinish;
        LatestFinish = durationInfo?.LatestFinish;
    }

    private EventVertex(int id, Activity milestoneActivity) : this(id, EventVertexType.Milestone, null) => MilestoneActivity = milestoneActivity;

    public bool IsMilestone => MilestoneActivity != null;

    public override bool Equals(object obj) => obj is EventVertex vertex && this == vertex;
    public override int GetHashCode() => Id.GetHashCode() ^ Type.GetHashCode();
    public static bool operator ==(EventVertex x, EventVertex y) => x.Id == y.Id;
    public static bool operator !=(EventVertex x, EventVertex y) => !(x == y);

    #region Factory Methods
    public static EventVertex CreateMilestone(int id, Activity milestoneActivity) => new(id, milestoneActivity);

    public static EventVertex CreateGraphStart(int id, IDurationInfo durationInfo) => new(id, EventVertexType.GraphStart, durationInfo);

    public static EventVertex CreateGraphEnd(int id, IDurationInfo durationInfo) => new(id, EventVertexType.GraphEnd, durationInfo);

    public static EventVertex Create(int id, IDurationInfo durationInfo) => new(id, EventVertexType.Normal, durationInfo);
    #endregion
}

public enum EventVertexType
{
    Normal,
    GraphStart,
    GraphEnd,
    Milestone
}
