using ActivityDiagram.Contracts.Model.Graph;

namespace ActivityDiagram.Contracts;

public interface IArrowGraphWriter
{
    void Write(ActivityArrowGraph graph);
}
