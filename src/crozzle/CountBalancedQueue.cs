using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace crozzle
{
	class CountBalancedQueue : IWorkspaceQueue
	{
		private readonly SortedDictionary<int, WorkspacePriorityQueue> _queues = new SortedDictionary<int, WorkspacePriorityQueue>();

		const int EachQueueLength = 80000;
		const int OverflowThreshold = EachQueueLength * 2 / 3;
		const int LengthWhereYouJustEmptyIt = 30;
		
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
					WorkspacePriorityQueue? wpq = null;
					if (!(
						_queues.TryGetValue(
							g.Key,
							out WorkspacePriorityQueue? wpq
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
								int n when n >= LengthWhereYouJustEmptyIt => maxReturnCount - rv.Count,
								_ =>
									Math.Min(
										maxReturnCount - rv.Count,
										Math.Max(wpq.Count + g.Count() - OverflowThreshold, 0)
									)
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
					if (kvp.Key >= LengthWhereYouJustEmptyIt)
					{
						rv.AddRange(
							wpq.Swap(
								Enumerable.Empty<WorkspaceNode>(), 
								maxCount - rv.Count
							)
						);
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
				foreach (var kvp in _queues)
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
