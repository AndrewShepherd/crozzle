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

		bool IWorkspaceQueue.IsEmpty => !(_queues.Any(kvp => !kvp.Value.IsEmpty));

		private readonly object mutex = new object();

		void IWorkspaceQueue.AddRange(IEnumerable<WorkspaceNode> workspaceNodes)
		{
			lock(mutex)
			{
				foreach (var n in workspaceNodes)
				{
					this.Push(n);
				}
			}
		}

		void Push(WorkspaceNode workspaceNode)
		{
			var key = workspaceNode.Workspace.Board.WordPlacements.Count;
			if (
				_queues.TryGetValue(
					key,
					out WorkspacePriorityQueue wpq
				)
			)
			{
				wpq.AddRange(new[] { workspaceNode });
			}
			else
			{
				wpq = new WorkspacePriorityQueue(EachQueueLength);
				wpq.AddRange(new[] { workspaceNode });
				_queues.Add(key, wpq);
			}
		}

		WorkspaceNode IWorkspaceQueue.Pop()
		{
			lock(mutex)
			{
				foreach (var kvp in _queues.Reverse())
				{
					var wpq = kvp.Value;
					if ((kvp.Key >= 22) && (!wpq.IsEmpty))
					{
						return wpq.Pop();
					}

					if (wpq.Count > EachQueueLength * 2 / 3)
					{
						return wpq.Pop();
					}
				}
				foreach (var kvp in _queues)
				{
					var wpq = kvp.Value;
					if (!wpq.IsEmpty)
					{
						return wpq.Pop();
					}
				}
				return null;
			}
		}

	}
}
