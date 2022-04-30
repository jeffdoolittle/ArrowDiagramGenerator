namespace ActivityDiagram.Contracts;

public interface IDurationInfo
{
    int EarliestStart { get; }

    int LatestStart { get; }

    int EarliestFinish { get; }

    int LatestFinish { get; }
}

public static class CriticalPathExtensions
{
    private class Durationinfo<T> : IDurationInfo
    {
        public IEnumerable<T> Predecessors { get; private set; }
        public IEnumerable<T> Successors { get; private set; }

        public int EarliestStart { get; set; }

        public int LatestStart { get; set; }

        public int EarliestFinish { get; set; }

        public int LatestFinish { get; set; }

        public Durationinfo(IEnumerable<T> predecessors, IEnumerable<T> successors)
        {
            Predecessors = predecessors;
            Successors = successors;
        }
    }

    private static IEnumerable<KeyValuePair<T, Durationinfo<T>>> OrderByDependencies<T>(IEnumerable<KeyValuePair<T, Durationinfo<T>>> list)
    {
        var processedPairs = new HashSet<T>();
        var totalCount = list.Count();
        var rc = new List<KeyValuePair<T, Durationinfo<T>>>(totalCount);
        while (rc.Count < totalCount)
        {
            var foundSomethingToProcess = false;
            foreach (var kvp in list)
            {
                if (!processedPairs.Contains(kvp.Key)
                    && kvp.Value.Predecessors.All(processedPairs.Contains))
                {
                    rc.Add(kvp);
                    _ = processedPairs.Add(kvp.Key);
                    foundSomethingToProcess = true;
                    yield return kvp;
                }
            }
            if (!foundSomethingToProcess)
            {
                throw new InvalidOperationException("Loop detected inside path");
            }
        }
    }

    private static void ForwardPass<T>(this IDictionary<T, Durationinfo<T>> list, Func<T, int> lengthSelector)
    {
        if (!list.Any())
        {
            return;
        }

        foreach (var item in list)
        {
            var predecessors = item.Value.Predecessors.ToArray();

            foreach (var predecessor in predecessors)
            {
                var predecessorDurationInfo = list[predecessor];

                if (item.Value.EarliestStart < predecessorDurationInfo.EarliestFinish)
                {
                    item.Value.EarliestStart = predecessorDurationInfo.EarliestFinish;
                }
            }
            var activityDuration = lengthSelector(item.Key);
            item.Value.EarliestFinish = item.Value.EarliestStart + activityDuration;
        }
    }

    private static void BackwardPass<T>(this IDictionary<T, Durationinfo<T>> list, Func<T, int> lengthSelector)
    {
        var reversedList = list.Reverse().ToArray();
        var isFirst = true;

        foreach (var node in reversedList)
        {
            if (isFirst)
            {
                node.Value.LatestFinish = node.Value.EarliestFinish;
                isFirst = false;
            }

            var successors = node.Value.Successors.ToArray();

            foreach (var successor in successors)
            {
                var successorDurationInfo = list[successor];

                if (node.Value.LatestFinish == 0)
                {
                    node.Value.LatestFinish = successorDurationInfo.LatestStart;
                }
                else if (node.Value.LatestFinish > successorDurationInfo.LatestStart)
                {
                    node.Value.LatestFinish = successorDurationInfo.LatestStart;
                }
            }

            node.Value.LatestStart = node.Value.LatestFinish - lengthSelector(node.Key);
        }
    }

    /// <summary>
    /// Set earliest and latest finish values for the specified type.
    /// </summary>
    public static void SetDurations<T>(this IEnumerable<T> list,
        Func<T, IEnumerable<T>> predecessorSelector,
        Func<T, int> lengthSelector,
        Action<(T Item, IDurationInfo DurationInfo)> setter)
    {
        var successors = list.GetSuccessors(predecessorSelector);
        var piList = list.ToDurationInfoDictionary(predecessorSelector, n => successors[n]);
        var orderedList = OrderByDependencies(piList).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        orderedList.ForwardPass(lengthSelector);
        orderedList.BackwardPass(lengthSelector);

        foreach (var item in orderedList)
        {
            setter((item.Key, item.Value));
        }
    }

    /// <summary>
    /// Calculate the critical path through a network.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="predecessorSelector"></param>
    /// <param name="lengthSelector"></param>
    /// <returns></returns>
    public static IEnumerable<T> CriticalPath<T>(this IEnumerable<T> list, Func<T, IEnumerable<T>> predecessorSelector, Func<T, int> lengthSelector)
    {
        var successors = list.GetSuccessors(predecessorSelector);
        var piList = list.ToDurationInfoDictionary(predecessorSelector, n => successors[n]);
        var orderedList = OrderByDependencies(piList).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        orderedList.ForwardPass(lengthSelector);
        orderedList.BackwardPass(lengthSelector);
        return orderedList
            .Where(
                kvp => (kvp.Value.EarliestFinish - kvp.Value.LatestFinish == 0)
                    && (kvp.Value.EarliestStart - kvp.Value.LatestStart == 0))
            .Select(n => n.Key)
            .ToArray();
    }

    private static IDictionary<T, IEnumerable<T>> GetSuccessors<T>(this IEnumerable<T> list, Func<T, IEnumerable<T>> predecessorSelector)
    {
        var rc = new Dictionary<T, IEnumerable<T>>();
        foreach (var item in list)
        {
            // Ensure the item is included, even if it is dangling (no item declares it as a predecessor)
            if (!rc.ContainsKey(item))
            {
                rc.Add(item, new List<T>());
            }

            // Iterate the items predecessors and add the current item
            foreach (var predecessor in predecessorSelector(item))
            {
                if (!rc.ContainsKey(predecessor))
                {
                    rc.Add(predecessor, new List<T>());
                }

                var predecessorSuccessorList = (List<T>)rc[predecessor];

                predecessorSuccessorList.Add(item);
            }
        }
        return rc;
    }

    private static IDictionary<T, Durationinfo<T>> ToDurationInfoDictionary<T>(this IEnumerable<T> list, Func<T, IEnumerable<T>> predecessorSelector, Func<T, IEnumerable<T>> successorSelector) =>
        list.ToDictionary(
            item => item,
            item => new Durationinfo<T>(predecessorSelector(item), successorSelector(item)));
}
