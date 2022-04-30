namespace ActivityDiagram.Contracts.Model.Activities;

public class ActivityDependency
{
    public Activity Activity { get; private set; }
    public IReadOnlyList<int> Predecessors { get; private set; }
    public IReadOnlyList<int> Successors { get; private set; }
    public int? EarliestStart { get; set; }
    public int? LatestStart { get; set; }
    public int? EarliestFinish { get; set; }
    public int? LatestFinish { get; set; }

    public ActivityDependency(Activity activity, List<int> predecessors, List<int> successors)
    {
        Activity = activity;
        Predecessors = predecessors.AsReadOnly();
        Successors = successors.AsReadOnly();
    }
}
