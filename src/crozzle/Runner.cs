using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace crozzle
{
	public class Runner
	{
		public static IEnumerable<Workspace> SolveUsingSimpleRecursion(
			Workspace startWorkspace,
			INextStepGenerator generator,
			CancellationToken cancellationToken
		)
		{
			foreach(var child in generator.GenerateNextSteps(startWorkspace))
			{
				if(child.IsValid)
				{
					yield return child;
				}
				if(cancellationToken.IsCancellationRequested)
				{
					yield break;
				}
				foreach(var grandChild in SolveUsingSimpleRecursion(child, generator, cancellationToken))
				{
					yield return grandChild;
					if (cancellationToken.IsCancellationRequested)
					{
						yield break;
					}
				}
			}
		}

		private static List<WorkspaceNode> FetchFromQueue(
			IWorkspaceQueue wpq,
			int batchSize)
		{
			return wpq.Swap(Enumerable.Empty<WorkspaceNode>(), batchSize).ToList();
		}

		public static WorkspaceNode AsOneWorkspace(IEnumerable<WorkspaceNode> workspaceNodes)
		{
			var firstNode = workspaceNodes.First();
			if(workspaceNodes.Count() == 1)
			{
				return firstNode;
			}
			else
			{
				return new WorkspaceNode
				{
					Ancestry = firstNode.Ancestry,
					Workspace = firstNode.Workspace.ResetAllSlots(),
				};
			}
		}

		public static WorkspaceNode[] MakeDistinct(IEnumerable<WorkspaceNode> workspaceNodes)
		{
			var groupedByBoard = workspaceNodes
				.GroupBy(w => w.Workspace.Board)
				.ToArray();
			var result = new WorkspaceNode[groupedByBoard.Length];
			for(int i = 0; i < groupedByBoard.Length; ++i)
			{
				result[i] = AsOneWorkspace(groupedByBoard[i]);
			}
			return result;
		}

		static int identifier = 0;


		private static void SolveUsingQueue(
				IWorkspaceQueue wpq,
				int batchSize,
				INextStepGenerator nextStepGenerator,
				CancellationToken cancellationToken,
				BlockingCollection<Workspace> blockingCollection
			)
		{
			int queueIteration = 0;
			while ((!wpq.IsEmpty) && (!cancellationToken.IsCancellationRequested))
			{
				++queueIteration;
				//List<WorkspaceNode> wList = FetchFromQueueWithDiverseAncestry(wpq, batchSize);
				List<WorkspaceNode> wList = FetchFromQueue(wpq, batchSize);

				List<WorkspaceNode> childWorkspaces = new List<WorkspaceNode>();
				var workspaceEnumerator = wList.GetEnumerator();
				while (workspaceEnumerator.MoveNext())
				{
					var thisNode = workspaceEnumerator.Current;
					foreach (var ns in nextStepGenerator.GenerateNextSteps(thisNode.Workspace))
					{
						if (ns.IsValid)
						{
							childWorkspaces.Add(
								new WorkspaceNode
								{
									Workspace = ns,
									Ancestry = thisNode.Ancestry.Add(++identifier),
								}
							);
							blockingCollection.Add(ns);
						}
						else
						{
							foreach (var nsChild in ns.GetValidChildren(nextStepGenerator))
							{
								childWorkspaces.Add(
									new WorkspaceNode
									{
										Workspace = nsChild,
										Ancestry = thisNode.Ancestry.Add(++identifier),
									}
								);
								blockingCollection.Add(nsChild);
							}
						}
					}
					if (childWorkspaces.Count() > wpq.Capacity / 10)
					{
						// Abort this iteration
						while (workspaceEnumerator.MoveNext())
						{
							wpq.Swap(new[] { workspaceEnumerator.Current }, 0);
						}
						break;
					}
				}
				int countBefore = childWorkspaces.Count();

				var distinctWorkspaces = MakeDistinct(childWorkspaces);
				if (countBefore != distinctWorkspaces.Count())
				{
					int dummy = 3;
				}
				wpq.Swap(childWorkspaces.Distinct(), 0);
			}
		}

		public static IEnumerable<Workspace> SolveUsingQueue(
			IEnumerable<Workspace> startWorkspaces,
			int queueLength,
			int batchSize,
			INextStepGenerator nextStepGenerator,
			CancellationToken cancellationToken
		)
		{
			//IWorkspaceQueue wpq = new WorkspacePriorityQueue(queueLength);
			IWorkspaceQueue wpq = new CountBalancedQueue();
			wpq.Swap(
				startWorkspaces.Select(
					w => 
						new WorkspaceNode
						{ 
							Workspace = w,
							Ancestry = ImmutableList<int>.Empty.Add(identifier++), 
						}
				).ToList(),
				0
			);
			var blockingCollection = new BlockingCollection<Workspace>();
			for(int i = 0; i < 8; ++i)
			{
				Task.Run(
					() =>
					{
						while(!cancellationToken.IsCancellationRequested)
						{
							SolveUsingQueue(wpq, batchSize, nextStepGenerator, cancellationToken, blockingCollection);
						}
					}
				);
			}
			return new BlockingCollectionEnumerable<Workspace>(blockingCollection, cancellationToken);
		}

		private static Workspace GetFirstValidChild(Workspace workspace, INextStepGenerator nextStepGenerator)
		{
			foreach (var ns in nextStepGenerator.GenerateNextSteps(workspace))
			{
				if (ns.IsValid)
				{
					return ns;
				}
				foreach (var nsChild in ns.GetValidChildren(nextStepGenerator))
				{
					return nsChild;
				}
			}
			return null;
		}

		public static IEnumerable<Workspace> SolveRecursively(IEnumerable<Workspace> workspaces, INextStepGenerator nextStepGenerator)
		{
			foreach (var w in workspaces)
			{
				var nextSteps = nextStepGenerator.GenerateNextSteps(w)
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
							nextStepGenerator
						)
					)
						yield return w2;
				}
			}
		}
	}
}
