using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace crozzle
{
	public class Runner
	{
		public static IEnumerable<Workspace> SolveUsingQueue(
			IEnumerable<Workspace> startWorkspaces,
			int queueLength,
			int batchSize,
			CancellationToken cancellationToken
		)
		{
			WorkspacePriorityQueue wpq = new WorkspacePriorityQueue(queueLength);
			foreach (var workspace in startWorkspaces)
			{
				wpq.Push(workspace);
			}

			while ((!wpq.IsEmpty) && (!cancellationToken.IsCancellationRequested))
			{
				List<Workspace> wList = new List<Workspace>(batchSize);
				wList.Add(wpq.Pop());
				while (wList.Count < batchSize && !wpq.IsEmpty)
				{
					var poppedValue = wpq.Pop();
					var lastValue = wList[wList.Count - 1];
					if (lastValue.Equals(poppedValue))
					{
						continue;
					}
					else
					{
						int dummy = 3;
					}
					wList.Add(poppedValue);
				}
				List<Workspace> childWorkspaces = new List<Workspace>();
				foreach (var thisWorkspace in wList)
				{
					var nextSteps = thisWorkspace.GenerateNextSteps().ToList();
					if (nextSteps.Any())
					{
						foreach (var ns in nextSteps)
						{
							if (ns.IsValid)
							{
								childWorkspaces.Add(ns);
							}
							else
							{
								foreach (var nsChild in GetValidChildren(ns))
								{
									childWorkspaces.Add(nsChild);
								}
							}
						}
					}
					else
					{
						if (thisWorkspace.IsValid)
						{
							yield return thisWorkspace;
						}
					}
				}
				wpq.AddRange(childWorkspaces.Distinct());
			}
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
