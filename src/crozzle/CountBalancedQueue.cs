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
			foreach(var workspaceNode in workspaceNodes)
			{
				Push(workspaceNode);
			}
			return this.Pop(maxReturnCount);
		}

		void Push(WorkspaceNode workspaceNode)
		{
			var key = workspaceNode.Workspace.Board.WordPlacements.Count;
			lock(mutex)
			{
				if (
					_queues.TryGetValue(
						key,
						out WorkspacePriorityQueue wpq
					)
				)
				{
					wpq.Swap(new[] { workspaceNode }, 0);
				}
				else
				{
					wpq = new WorkspacePriorityQueue(EachQueueLength);
					wpq.Swap(new[] { workspaceNode }, 0);
					_queues.Add(key, wpq);
				}
			}
		}

		IEnumerable<WorkspaceNode> Pop(int maxCount)
		{
			List<WorkspaceNode> rv = new List<WorkspaceNode>();
			lock(mutex)
			{
				foreach(var kvp in _queues.Reverse())
				{
					var wpq = kvp.Value;
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
