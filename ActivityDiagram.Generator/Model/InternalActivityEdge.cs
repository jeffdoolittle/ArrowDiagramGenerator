using System.Diagnostics;
using QuikGraph;

namespace ActivityDiagram.Generator.Model;

[DebuggerDisplay("{Source}->{Target}")]
internal class InternalActivityEdge : IEdge<InternalEventVertex>
{
    public InternalActivityEdge(InternalEventVertex source, InternalEventVertex target, bool isCritical, int? activityId = null)
    {
        Source = source;
        Target = target;
        ActivityId = activityId;
        IsCritical = isCritical;
    }

    public InternalEventVertex Source { get; }
    public InternalEventVertex Target { get; }
    public int? ActivityId { get; }
    public bool IsCritical { get; }

    public override string ToString() => $"{Source}-{ActivityId}->{Target}";
}
