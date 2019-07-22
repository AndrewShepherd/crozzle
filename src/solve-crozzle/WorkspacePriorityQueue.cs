using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	class WorkspacePriorityQueue
	{
		Workspace[] _workspaces = new Workspace[1000000];
		int _length = 0;
		public Workspace Pop()
		{
			var result = _workspaces[0];
			(_workspaces[0], _workspaces[_length - 1], _length) = (_workspaces[_length - 1], null, --_length);
	
			int i = 0;
			while ((i*2+1) < _length)
			{
				(int j, int k) = ((i * 2) + 1, (i * 2) + 2);
				int l = _workspaces[j].Score > (_workspaces[k]?.Score ?? 0) ? j : k;
				if(_workspaces[l].Score <= _workspaces[i].Score)
				{
					break;
				}
				else
				{
					(_workspaces[i], _workspaces[l], i) = (_workspaces[l], _workspaces[i], l);
				}
			}
			return result;
		}

		public void Push(Workspace workspace)
		{
			int i = _length++;
			_workspaces[i] = workspace;
			var thisScore = workspace.Score;
			while(i != 0)
			{
				var j = (i - 1) / 2;
				if (_workspaces[i].Score > thisScore)
				{
					return;
				}
				(_workspaces[i], _workspaces[j], i) = (_workspaces[j], _workspaces[i], j);
			}			
		}

		public void AddRange(IEnumerable<Workspace> values)
		{
			foreach (var value in values)
				Push(value);
		}

		public bool IsEmpty => _length == 0;
	}
}
