using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GroboTrace
{
    internal class MethodCallNode
    {
        public MethodCallNode(MethodCallNode parent, ulong handle, MethodInfo method)
        {
            this.parent = parent;
            this.handle = handle;
            Method = method;
            handles = new ulong[1];
            children = new MethodCallNode[1];
        }

        public MethodCallNode StartMethod(ulong methodHandle, MethodInfo method)
        {
            var index = methodHandle % (uint)handles.Length;
            if(handles[index] == methodHandle)
                return children[index];
            if(handles[index] != 0)
            {
                // rebuild table
                index = Rebuild(methodHandle);
            }
            if(handles[index] == 0)
            {
                handles[index] = methodHandle;
                children[index] = new MethodCallNode(this, methodHandle, method);
            }
            return children[index];
        }

        public MethodCallNode FinishMethod(ulong methodHandle, long elapsed)
        {
            if(methodHandle != handle)
                throw new InvalidOperationException();
            ++Calls;
            Ticks += elapsed;
            return parent;
        }

        public MethodStatsNode GetStats(long totalTicks)
        {
            return new MethodStatsNode
                {
                    MethodStats = new MethodStats
                        {
                            Method = Method,
                            Calls = Calls,
                            Ticks = Ticks,
                            Percent = totalTicks == 0 ? 0.0 : Ticks * 100.0 / totalTicks
                        },
                    Children = Children.Select(child =>
                        {
                            var childStats = child.GetStats(totalTicks);
                            return childStats;
                        }).OrderByDescending(stats => stats.MethodStats.Ticks).
                                        ToArray()
                };
        }

        public void GetStats(Dictionary<MethodInfo, MethodStats> statsDict)
        {
            var selfTicks = Ticks;
            foreach(var child in Children)
            {
                child.GetStats(statsDict);
                selfTicks -= child.Ticks;
            }
            var method = Method.IsGenericMethod ? Method.GetGenericMethodDefinition() : Method;
            MethodStats stats;
            if(!statsDict.TryGetValue(method, out stats))
                statsDict.Add(method, new MethodStats {Calls = Calls, Method = method, Ticks = selfTicks});
            else
            {
                stats.Calls += Calls;
                stats.Ticks += selfTicks;
            }
        }

        public void ClearStats()
        {
            Calls = 0;
            Ticks = 0;
            foreach(var child in Children)
                child.ClearStats();
        }

        public MethodInfo Method { get; set; }
        public int Calls { get; set; }
        public long Ticks { get; set; }

        public IEnumerable<MethodCallNode> Children { get { return children.Where(node => node != null && node.Calls > 0); } }

        private uint Rebuild(ulong newHandle)
        {
            var values = new List<ulong>();
            for(int i = 0; i < handles.Length; ++i)
            {
                if(handles[i] != 0)
                    values.Add(handles[i]);
            }
            values.Add(newHandle);
            var length = (uint)handles.Length;
            while(true)
            {
                ++length;
                var was = new bool[length];
                bool ok = true;
                for(int i = 0; i < values.Count; ++i)
                {
                    var index = values[i] % length;
                    if(was[index])
                    {
                        ok = false;
                        break;
                    }
                    was[index] = true;
                }
                if(ok) break;
            }
            var newHandles = new ulong[length];
            var newChildren = new MethodCallNode[length];
            for(int i = 0; i < handles.Length; ++i)
            {
                if(handles[i] != 0)
                {
                    var index = handles[i] % length;
                    newHandles[index] = handles[i];
                    newChildren[index] = children[i];
                }
            }
            handles = newHandles;
            children = newChildren;
            return (uint)(newHandle % length);
        }

        private readonly ulong handle;
        private readonly MethodCallNode parent;
        private MethodCallNode[] children;
        private ulong[] handles;
    }
}