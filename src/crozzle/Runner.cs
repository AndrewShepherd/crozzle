using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace crozzle
{
	public class Runner
	{
		public static IEnumerable<Workspace> SolveUsingSimpleRecursion(
			Workspace startWorkspace,
			CancellationToken cancellationToken
		)
		{
			foreach(var child in startWorkspace.GenerateNextSteps())
			{
				if(child.IsValid)
				{
					yield return child;
				}
				if(cancellationToken.IsCancellationRequested)
				{
					yield break;
				}
				foreach(var grandChild in SolveUsingSimpleRecursion(child, cancellationToken))
				{
					yield return grandChild;
					if (cancellationToken.IsCancellationRequested)
					{
						yield break;
					}
				}
			}
		}

		private static List<WorkspaceNode> FetchFromQueue(WorkspacePriorityQueue wpq, int batchSize)
		{
			Dictionary<int, int> nodeCounts = new Dictionary<int, int>();

			List<WorkspaceNode> rejects = new List<WorkspaceNode>();

			var wList = new List<WorkspaceNode>(batchSize);
			int maxSizeForLevel2 = (wpq.Count < (wpq.Capacity / 2))
				? batchSize / 2
				: int.MaxValue;
			maxSizeForLevel2 = batchSize / 2;
			while (wList.Count < batchSize && !wpq.IsEmpty)
			{
				var candidate = wpq.Pop();
				bool passes = true;
				if (candidate.Ancestry.Count > 2)
				{
					var wId = candidate.Ancestry[2];
					if (nodeCounts.TryGetValue(wId, out var thisCount))
					{
						if (thisCount >= maxSizeForLevel2)
						{
							passes = false;
						}
						else
						{
							nodeCounts[wId] = ++thisCount;
							if(thisCount == maxSizeForLevel2)
							{
								maxSizeForLevel2 = Math.Max(2, maxSizeForLevel2 / 2);
							}
						}
					}
					else
					{
						nodeCounts[wId] = 1;
					}
				}
				if (passes)
				{
					wList.Add(candidate);
				}
				else
				{
					rejects.Add(candidate);
				}
			}
			foreach(var r in rejects)
			{
				wpq.Push(r);
			}
			return wList;
		}


		public static IEnumerable<Workspace> SolveUsingQueue(
			IEnumerable<Workspace> startWorkspaces,
			int queueLength,
			int batchSize,
			CancellationToken cancellationToken
		)
		{
			int identifier = 0;
			WorkspacePriorityQueue wpq = new WorkspacePriorityQueue(queueLength);
			foreach (
				var workspace in startWorkspaces
			)
			{
				wpq.Push(
					new WorkspaceNode
					{
						Workspace = workspace,
						Ancestry = ImmutableList<int>.Empty.Add(identifier++)
					}
				);
			}
			int queueIteration = 0;
			while ((!wpq.IsEmpty) && (!cancellationToken.IsCancellationRequested))
			{
				++queueIteration;
				List<WorkspaceNode> wList = FetchFromQueue(wpq, batchSize);
				List<WorkspaceNode> childWorkspaces = new List<WorkspaceNode>();
				var workspaceEnumerator = wList.GetEnumerator();
				while(workspaceEnumerator.MoveNext())
				{
					var thisNode= workspaceEnumerator.Current;
					foreach (var ns in thisNode.Workspace.GenerateNextSteps())
					{
						if (ns.IsValid)
						{
							yield return ns; // Partially complete, but interesting
							childWorkspaces.Add(
								new WorkspaceNode
								{
									Workspace = ns,
									Ancestry = thisNode.Ancestry.Add(++identifier),
								}
							);
						}
						else
						{
							foreach (var nsChild in GetValidChildren(ns))
							{
								yield return nsChild;
								childWorkspaces.Add(
									new WorkspaceNode
									{
										Workspace = ns,
										Ancestry = thisNode.Ancestry.Add(++identifier),
									}
								);
							}
						}
					}
					if(childWorkspaces.Count() > queueLength/10)
					{
						// Abort this iteration
						while(workspaceEnumerator.MoveNext())
						{
							wpq.AddRange(
								new[]
								{
									workspaceEnumerator.Current
								}
							);
						}
						break;
					}
				}
				int countBefore = childWorkspaces.Count();
				var distinctWorkspaces = childWorkspaces.Distinct().ToList();
				if(countBefore != distinctWorkspaces.Count())
				{
					int dummy = 3;
				}
				if((wpq.Count + distinctWorkspaces.Count()) > wpq.Capacity)
				{
					PurgeQueue(wpq);
				}
				wpq.AddRange(childWorkspaces.Distinct());
			}
		}

		private static void PurgeQueue(WorkspacePriorityQueue wpq)
		{
			wpq.RemoveItems(
				node =>
				{
					foreach(var ns in node.Workspace.GenerateNextSteps())
					{
						if(ns.IsValid)
						{
							return false;
						}
						foreach(var nsChild in GetValidChildren(ns))
						{
							return false;
						}
					}
					return true;
				}
			);
		}

		public static IEnumerable<Workspace> GetValidChildren(Workspace workspace)
		{
			foreach (var nextStep in workspace.GenerateNextSteps())
			{
				if (nextStep.IsValid)
				{
					yield return nextStep;
				}
				else
				{
					foreach (var child in GetValidChildren(nextStep))
					{
						yield return child;
					}
				}
			}
		}

		public static IEnumerable<Workspace> SolveRecursively(IEnumerable<Workspace> workspaces)
		{
			foreach (var w in workspaces)
			{
				var nextSteps = w.GenerateNextSteps()
					.ToList();
				if (!nextSteps.Any())
				{
					if (w.IsValid)
						yield return w;
				}
				else
				{
					foreach (
						var w2 in SolveRecursively(
							nextSteps.OrderByDescending(ns => ns.PotentialScore)
						)
					)
						yield return w2;
				}
			}
		}
	}
}
