using System;
using System.Collections.Generic;
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
			int queueIteration = 0;
			while ((!wpq.IsEmpty) && (!cancellationToken.IsCancellationRequested))
			{
				++queueIteration;
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
					bool atLeastOneChild = false;
					foreach (var ns in thisWorkspace.GenerateNextSteps())
					{
						atLeastOneChild = true;
						//childWorkspaces.Add(ns);
						//continue;	
						if (ns.IsValid)
						{
							yield return ns; // Partially complete, but interesting
							childWorkspaces.Add(ns);
						}
						else
						{
							foreach (var nsChild in GetValidChildren(ns))
							{
								yield return nsChild;
								childWorkspaces.Add(nsChild);
							}
						}
						// A hack to get around the duplicates being generated
						if (childWorkspaces.Count() > 300000)
						{
							childWorkspaces = childWorkspaces.Distinct().ToList();
							if(childWorkspaces.Count() > 300000)
							{
								wpq.AddRange(childWorkspaces);
								childWorkspaces.Clear();
							}
						}
					}
					if(!atLeastOneChild)
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
