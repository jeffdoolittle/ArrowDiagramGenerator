using ActivityDiagram.Contracts.Model.Activities;

namespace ActivityDiagram.Contracts.Model.Graph;

public class ActivityEdge
{
    public int Id { get; private set; }
    public EventVertex Source { get; private set; }
    public EventVertex Target { get; private set; }
    public Activity Activity { get; private set; }
    public bool IsCritical { get; private set; }

    public ActivityEdge(int id, EventVertex source, EventVertex target) : this(id, source, target, null, false) { }

    public ActivityEdge(int id, EventVertex source, EventVertex target, Activity activity, bool isCritical)
    {
        Id = id;
        Source = source;
        Target = target;
        Activity = activity;
        IsCritical = isCritical;
    }

    public override bool Equals(object obj) => obj is ActivityEdge edge && this == edge;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(ActivityEdge x, ActivityEdge y) => x.Id == y.Id;
    public static bool operator !=(ActivityEdge x, ActivityEdge y) => !(x == y);
}
