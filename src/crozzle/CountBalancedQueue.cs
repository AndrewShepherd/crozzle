using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace crozzle
{
	class CountBalancedQueue : IWorkspaceQueue
	{
		private readonly SortedDictionary<int, WorkspacePriorityQueue> _queues = new SortedDictionary<int, WorkspacePriorityQueue>();

		const int EachQueueLength = 40000;
		
		int IWorkspaceQueue.Capacity => 31*EachQueueLength;

		int IWorkspaceQueue.Count => _queues.Select(kvp => kvp.Value.Count).Sum();

		bool IWorkspaceQueue.IsEmpty
		{
			get
			{
				lock(mutex)
				{
					return !(_queues.Any(kvp => !kvp.Value.IsEmpty));
				}
			}
		}

		private readonly object mutex = new object();

		IEnumerable<WorkspaceNode> IWorkspaceQueue.Swap(IEnumerable<WorkspaceNode> workspaceNodes, int maxReturnCount)
		{
			var grouped = workspaceNodes.GroupBy(n => n.Workspace.Board.WordPlacements.Count).ToList();
			List<WorkspaceNode> rv = new List<WorkspaceNode>();
			foreach(var g in grouped)
			{
				lock(mutex)
				{
					WorkspacePriorityQueue wpq = null;
					if (!(
						_queues.TryGetValue(
							g.Key,
							out wpq
						)
					))
					{
						wpq = new WorkspacePriorityQueue(EachQueueLength);
						_queues.Add(g.Key, wpq);
					}
					
					rv.AddRange(
						wpq.Swap(
							g,
							g.Key switch
							{
								int n when n >= 22 => maxReturnCount - rv.Count,
								_ when wpq.Count > EachQueueLength * 2 / 3 => maxReturnCount - rv.Count,
								_ => 0
							}
						)
					);
				}
			}
			if(rv.Count < maxReturnCount)
			{
				rv.AddRange(this.Pop(maxReturnCount - rv.Count));
			}
			return rv;
		}

		IEnumerable<WorkspaceNode> Pop(int maxCount)
		{
			List<WorkspaceNode> rv = new List<WorkspaceNode>();
			lock(mutex)
			{
				foreach(var kvp in _queues.Reverse())
				{
					var wpq = kvp.Value;
					if(wpq.IsEmpty)
					{
						continue;
					}
					if (kvp.Key >= 22)
					{
						rv.AddRange(wpq.Swap(Enumerable.Empty<WorkspaceNode>(), maxCount - rv.Count));
					}
					if(rv.Count >= maxCount)
					{
						return rv;
					}
					if (wpq.Count > EachQueueLength * 2 / 3)
					{
						rv.AddRange(
							wpq.Swap(
								Enumerable.Empty<WorkspaceNode>(),
								maxCount - rv.Count
							)
						);
					}
					if (rv.Count >= maxCount)
					{
						return rv;
					}
				}
				foreach (var kvp in _queues.Reverse())
				{
					var wpq = kvp.Value;
					rv.AddRange(
						wpq.Swap(
							Enumerable.Empty<WorkspaceNode>(),
							maxCount - rv.Count
						)
					);
					if (rv.Count >= maxCount)
					{
						return rv;
					}
				}
				return rv;
			}
		}

	}
}
