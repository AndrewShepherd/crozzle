using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	interface IWorkspaceQueue
	{
		IEnumerable<WorkspaceNode> Swap(IEnumerable<WorkspaceNode> workspaceNodes, int maxReturnCount);

		int Capacity { get; }

		int Count { get;  }

		bool IsEmpty { get; }
	}
}
