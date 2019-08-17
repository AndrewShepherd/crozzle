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

		private void SwapUp(int i)
		{
			if (i == 0)
				return;
			var j = (i - 1) / 2;
			switch(Compare(_workspaces[j], _workspaces[i]))
			{
				case 1:
					(_workspaces[i], _workspaces[j]) = (_workspaces[j], _workspaces[i]);
					SwapUp(j);
					break;
				case -1:
					break;
				default:
					if(_workspaces[i].Equals(_workspaces[j]))
					{
						RemoveElementAt(i);
					}
					break;
			}
		}

		private void SwapDown(int i)
		{
			if((i*2+1) >= _length)
			{
				return;
			}
			(int j, int k) = ((i * 2) + 1, (i * 2) + 2);
			int l;
			switch (Compare(_workspaces[j], _workspaces[k]))
			{
				case -1:
					l = j;
					break;
				case 1:
					l = k;
					break;
				default:
					if(_workspaces[j].Equals(_workspaces[k]))
					{
						RemoveElementAt(k);
						SwapDown(i);
						return;
					}
					else
					{
						l = k;
					}
					break;
			}
			switch (Compare(_workspaces[l], _workspaces[i]))
			{
				case -1:
					(_workspaces[i], _workspaces[l], i) = (_workspaces[l], _workspaces[i], l);
					SwapDown(l);
					break;
				case 0:
					bool areEqual = (_workspaces[i].Equals(_workspaces[l]));
					if (areEqual)
					{
						// They are equal! what do we do?
						RemoveElementAt(l);
						return;
					}
					break;
				default:
					return;
			}

		}

		private void RemoveElementAt(int index)
		{
			(
				_workspaces[index],
				_workspaces[_length - 1],
				_length
			) = (
				_workspaces[_length - 1],
				null,
				_length - 1
			);
			SwapDown(index);
		}

		public Workspace Pop()
		{
			var result = _workspaces[0];
			RemoveElementAt(0);
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
				if (Compare(_workspaces[i], workspace) < 0)
				{
					// Going off the edge here
					return;
				}
			}
			_workspaces[i] = workspace;
			SwapUp(i);		
		}

		public void AddRange(IEnumerable<Workspace> values)
		{
			foreach (var value in values)
				Push(value);
		}

		public bool IsEmpty => _length == 0;
	}
}
