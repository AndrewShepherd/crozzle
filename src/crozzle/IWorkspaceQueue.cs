using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	interface IWorkspaceQueue
	{
		void AddRange(IEnumerable<WorkspaceNode> workspaceNodes);

		WorkspaceNode Pop();

		int Capacity { get; }

		int Count { get;  }

		bool IsEmpty { get; }
	}
}
