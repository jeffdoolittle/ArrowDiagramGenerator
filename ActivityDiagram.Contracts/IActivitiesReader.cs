using ActivityDiagram.Contracts.Model.Activities;

namespace ActivityDiagram.Contracts;

public interface IActivitiesReader
{
    IEnumerable<ActivityDependency> Read();
}
