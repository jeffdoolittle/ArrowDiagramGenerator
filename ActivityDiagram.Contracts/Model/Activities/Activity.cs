namespace ActivityDiagram.Contracts.Model.Activities;

public class Activity
{
    public int Id { get; private set; }
    public int? Duration { get; private set; }
    public int? TotalSlack { get; private set; }
    public string Name { get; private set; }

    public Activity(int id) :
        this(id, null, null, "")
    {
    }

    public Activity(int id, int? duration, int? totalSlack, string name)
    {
        Id = id;
        Duration = duration;
        TotalSlack = totalSlack;
        Name = name;
    }

    public bool IsCritical => TotalSlack == 0;

    public override bool Equals(object obj) => obj is Activity activity && this == activity;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(Activity x, Activity y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        // If one is null, but not both, return false.
        if ((x is null) || (y is null))
        {
            return false;
        }

        return x.Id == y.Id;
    }
    public static bool operator !=(Activity x, Activity y) => !(x == y);
}
