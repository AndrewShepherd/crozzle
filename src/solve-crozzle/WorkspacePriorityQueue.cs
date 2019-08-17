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

		public static int Compare(Workspace w1, Workspace w2)
		{
			if((w1?.PotentialScore ?? 0) > (w2?.PotentialScore ?? 0))
			{
				return -1;
			}
			if((w2?.PotentialScore ?? 0) > (w1?.PotentialScore ?? 0))
			{
				return 1;
			}
			return w1.GetHashCode().CompareTo(w2.GetHashCode());
		}

		public Workspace Pop()
		{
			var result = _workspaces[0];
			(
				_workspaces[0],
				_workspaces[_length - 1],
				_length
			) = (
				_workspaces[_length - 1],
				null,
				--_length
			);
	
			int i = 0;
			while ((i*2+1) < _length)
			{
				(int j, int k) = ((i * 2) + 1, (i * 2) + 2);
				int l = Compare(_workspaces[j], _workspaces[k]) < 0 ? j : k;
				if (Compare(_workspaces[l], _workspaces[i]) >= 0)
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
				if(Compare(_workspaces[i], workspace) < 0)
					return;
			}
			_workspaces[i] = workspace;
			while(i != 0)
			{
				var j = (i - 1) / 2;
				if (j >= _workspaces.Length)
					return;
				if(Compare(_workspaces[j], workspace) < 0)
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
