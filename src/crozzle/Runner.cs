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
			GenerationSettings generationSettings,
			CancellationToken cancellationToken
		)
		{
			foreach(var child in startWorkspace.GenerateNextSteps(generationSettings))
			{
				if(child.IsValid)
				{
					yield return child;
				}
				if(cancellationToken.IsCancellationRequested)
				{
					yield break;
				}
				foreach(var grandChild in SolveUsingSimpleRecursion(child, generationSettings, cancellationToken))
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
			GenerationSettings generationSettings,
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
					foreach (var ns in thisNode.Workspace.GenerateNextSteps(generationSettings))
					{
						if (ns.IsValid)
						{
							var validChild = GetFirstValidChild(ns, generationSettings);
							if(validChild != null)
							{
								yield return validChild;
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
								yield return ns;
							}
						}
						else
						{
							foreach (var nsChild in ns.GetValidChildren(generationSettings))
							{
								var validGrandChild = GetFirstValidChild(nsChild, generationSettings);

								if (validGrandChild == null)
								{
									yield return nsChild;
								}
								else
								{
									yield return validGrandChild;
									childWorkspaces.Add(
										new WorkspaceNode
										{
											Workspace = nsChild,
											Ancestry = thisNode.Ancestry.Add(++identifier),
										}
									);
								}
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
				wpq.AddRange(childWorkspaces.Distinct());
			}
		}

		private static Workspace GetFirstValidChild(Workspace workspace, GenerationSettings generationSettings)
		{
			foreach (var ns in workspace.GenerateNextSteps(generationSettings))
			{
				if (ns.IsValid)
				{
					return ns;
				}
				foreach (var nsChild in ns.GetValidChildren(generationSettings))
				{
					return ns;
				}
			}
			return null;
		}

		public static IEnumerable<Workspace> SolveRecursively(IEnumerable<Workspace> workspaces, GenerationSettings generationSettings)
		{
			foreach (var w in workspaces)
			{
				var nextSteps = w.GenerateNextSteps(generationSettings)
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
							nextSteps.OrderByDescending(ns => ns.PotentialScore),
							generationSettings
						)
					)
						yield return w2;
				}
			}
		}
	}
}
