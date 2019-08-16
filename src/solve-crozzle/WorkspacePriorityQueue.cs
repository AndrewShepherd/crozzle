using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	class WorkspacePriorityQueue
	{
		readonly Workspace[] _workspaces; 
		int _length = 0;
		public WorkspacePriorityQueue(int queueLength)
		{
			_workspaces = new Workspace[queueLength];
		}

		public int Count => _length;

		public Workspace Pop()
		{
			var result = _workspaces[0];
			(_workspaces[0], _workspaces[_length - 1], _length) = (_workspaces[_length - 1], null, --_length);
	
			int i = 0;
			while ((i*2+1) < _length)
			{
				(int j, int k) = ((i * 2) + 1, (i * 2) + 2);
				int l = _workspaces[j].PotentialScore > (_workspaces[k]?.PotentialScore ?? 0) ? j : k;
				if(_workspaces[l].PotentialScore <= _workspaces[i].PotentialScore)
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
			int i;
			if(_length < _workspaces.Length)
			{
				i = _length++;
			}
			else
			{
				i = _workspaces.Length - 1;
				if (_workspaces[i].PotentialScore > workspace.PotentialScore)
					return;
			}
			_workspaces[i] = workspace;
			var thisPotentialScore = workspace.PotentialScore;
			while(i != 0)
			{
				var j = (i - 1) / 2;
				if (j >= _workspaces.Length)
					return;
				if (_workspaces[j].PotentialScore > thisPotentialScore)
					return;
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
