using ActivityDiagram.Contracts;

namespace ActivityDiagram.Generator.Model;

internal class InternalEventVertex : IDurationInfo
{
    private readonly string _id;

    private InternalEventVertex(string id, int activityId, ActivityVertexType type, bool critical)
    {
        ActivityId = activityId;
        Type = type;
        IsCritical = critical;
        _id = id;
    }

    public string Id => ToString();

    public int ActivityId { get; }

    public ActivityVertexType Type { get; }

    public bool IsCritical { get; }
    public int? EarliestStart { get; set; }
    public int? LatestStart { get; set; }
    public int? EarliestFinish { get; set; }
    public int? LatestFinish { get; set; }

    int IDurationInfo.EarliestStart => EarliestStart ?? 0;

    int IDurationInfo.LatestStart => LatestStart ?? 0;

    int IDurationInfo.EarliestFinish => EarliestFinish ?? 0;

    int IDurationInfo.LatestFinish => LatestFinish ?? 0;

    public override bool Equals(object obj) => obj is InternalEventVertex vertex && this == vertex;
    public override int GetHashCode() => _id.GetHashCode();
    public static bool operator ==(InternalEventVertex x, InternalEventVertex y) => x._id == y._id;
    public static bool operator !=(InternalEventVertex x, InternalEventVertex y) => !(x == y);

    public override string ToString() => _id;

    public static InternalEventVertex Create(int activityId, ActivityVertexType type, bool critical) => new(FormatId(activityId, type), activityId, type, critical);

    private static string FormatId(int activityId, ActivityVertexType type)
    {
        if (type == ActivityVertexType.ActivityStart)
        {
            return "S" + activityId;
        }
        else
        {
            return "E" + activityId;
        }
    }
}

public enum ActivityVertexType
{
    ActivityStart,
    ActivityEnd
}
