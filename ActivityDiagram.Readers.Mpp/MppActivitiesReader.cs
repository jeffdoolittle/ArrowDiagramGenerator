using ActivityDiagram.Contracts;
using ActivityDiagram.Contracts.Model.Activities;
using net.sf.mpxj;
using net.sf.mpxj.MpxjUtilities;
using net.sf.mpxj.reader;

namespace ActivityDiagram.Readers.Mpp;

public class MppActivitiesReader : IActivitiesReader
{
    private readonly string _filename;

    public MppActivitiesReader(string filename) => _filename = filename;
    public IEnumerable<ActivityDependency> Read()
    {
        var reader = ProjectReaderUtility.getProjectReader(_filename);
        var mpx = reader.read(_filename);


        var actDependnecies = new List<ActivityDependency>();
        foreach (net.sf.mpxj.Task task in mpx.Tasks.ToIEnumerable())
        {
            var id = task.ID.intValue();
            var duration = task.Duration.Duration;
            var totalSlack = task.TotalSlack.Duration;
            var name = task.Name;

            var predecessors = new List<int>();
            var preds = task.Predecessors;
            if (preds != null && !preds.isEmpty())
            {
                foreach (Relation pred in preds.ToIEnumerable())
                {
                    predecessors.Add(pred.TargetTask.ID.intValue());
                }
            }

            var successors = new List<int>();
            var succs = task.Successors;
            if (succs != null && !succs.isEmpty())
            {
                foreach (Relation succ in succs.ToIEnumerable())
                {
                    successors.Add(succ.TargetTask.ID.intValue());
                }
            }

            actDependnecies.Add(
                new ActivityDependency(
                    new Activity(id, Convert.ToInt32(duration), Convert.ToInt32(totalSlack), name),
                        predecessors,
                        successors
                    )
                );
        }

        return actDependnecies;
    }
}
