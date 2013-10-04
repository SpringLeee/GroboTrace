using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GroboTrace
{
    internal class MethodCallTree
    {
        public MethodCallTree()
        {
            root = new MethodCallNode(null, 0, null);
            current = root;
            startTicks = TracingWrapper.GetTicks();
        }

        public void StartMethod(ulong methodHandle, MethodInfo method)
        {
            current = current.StartMethod(methodHandle, method);
        }

        public void FinishMethod(ulong methodHandle, long elsapsed)
        {
            current = current.FinishMethod(methodHandle, elsapsed);
        }

        public MethodStatsNode GetStatsAsTree(long endTicks)
        {
            var elapsedTicks = endTicks - startTicks;
            MethodStatsNode result = root.GetStats(elapsedTicks);
            result.MethodStats.Percent = 100.0;
            return result;
        }

        public List<MethodStats> GetStatsAsList(long endTicks)
        {
            var elapsedTicks = endTicks - startTicks;
            var statsDict = new Dictionary<MethodInfo, MethodStats>();
            foreach(var child in root.Children)
                child.GetStats(statsDict);
            var result = statsDict.Values.Concat(new[] {new MethodStats {Calls = 1, Ticks = elapsedTicks - statsDict.Values.Sum(node => node.Ticks)}}).OrderByDescending(stats => stats.Ticks).ToList();
            foreach(var stats in result)
                stats.Percent = stats.Ticks * 100.0 / elapsedTicks;
            return result;
        }

        public void ClearStats()
        {
            root.ClearStats();
            startTicks = TracingWrapper.GetTicks();
        }

        private readonly MethodCallNode root;
        private MethodCallNode current;
        private long startTicks;
    }
}