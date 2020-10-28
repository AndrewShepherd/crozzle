namespace crozzle
{
	using System;
	using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;


	public class WorkspaceNode
	{
		public Workspace Workspace { get; set; } = Workspace.Empty;
		public ImmutableList<int> Ancestry = ImmutableList<int>.Empty;

		public override bool Equals(object? obj)
		{
			if(object.ReferenceEquals(this, obj))
			{
				return true;
			}
			if (
				(obj is WorkspaceNode wn)
				&& (wn.Workspace.Equals(this.Workspace))
			)
			{
				return true;
			}
			return false;
		}

		public override int GetHashCode() => this.Workspace.GetHashCode();
	}


	public class WorkspacePriorityQueue : IWorkspaceQueue
	{
		readonly WorkspaceNode?[] _workspaces; 
		int _length = 0;
		public WorkspacePriorityQueue(int queueLength)
		{
			_workspaces = new WorkspaceNode[queueLength];
		}

		public int Capacity => _workspaces.Length;

		public override string ToString() =>
			$"{_length} elements. TopElement:{_workspaces[0]?.ToString() ?? "None"}";

		public int Count => _length;

		private static Func<Workspace, Workspace, int> CompareProperties<T>(Func<Workspace, T> f) where T : IComparable<T> =>
			(w1, w2) => f(w1).CompareTo(f(w2));

		private static Func<Workspace, Workspace, int>[] ComparisonFunctions = new[]
		{
			(w1, w2) => w2.Score.CompareTo(w1.Score), // Deliberately reversing them
			CompareProperties(_ => _.IncludedWords.Count()),
			CompareProperties(_ => _.Board),
			CompareProperties(_ => _.GetHashCode())
		};

		public static int Compare(Workspace? w1, Workspace? w2)
		{
			if(object.ReferenceEquals(w1, w2))
			{
				return 0;
			}
			if(w2 == null)
			{
				return -1;
			}
			if(w1 == null)
			{
				return 1;
			}
			foreach(var f in ComparisonFunctions)
			{
				var c = f(w1, w2);
				if(c != 0)
				{
					return c;
				}
			}
			return 0;
		}

		public static int Compare(WorkspaceNode? w1, WorkspaceNode? w2) =>
			Compare(w1?.Workspace, w2?.Workspace);

		private void SwapUp(int i)
		{
			if (i == 0)
				return;
			var j = (i - 1) / 2;
			switch(Compare(_workspaces[j], _workspaces[i]))
			{
				case int n when n > 0:
					(_workspaces[i], _workspaces[j]) = (_workspaces[j], _workspaces[i]);
					SwapUp(j);
					break;
				case int n when n < 0:
					break;
				default:
					RemoveElementAt(i);
					_workspaces[j] = ResetSlots(_workspaces[j]);
					break;
			}
		}

		private static WorkspaceNode ResetSlots(WorkspaceNode? workspaceNode) =>
			new WorkspaceNode
			{
				Ancestry = workspaceNode?.Ancestry ?? ImmutableList<int>.Empty,
				Workspace = (workspaceNode?.Workspace ?? Workspace.Empty).ResetAllSlots(),
			};

		private void SwapDown(int i)
		{
			if((i*2+1) >= _length)
			{
				return;
			}
			(
				int j,
				int k
			) = (
				(i * 2) + 1,
				Math.Min((i * 2) + 2, _length-1)
			);
			int l;
			switch (Compare(_workspaces[j], _workspaces[k]))
			{
				case int n when n < 0:
					l = j;
					break;
				case int n when n > 0:
					l = k;
					break;
				default:
					RemoveElementAt(k);
					_workspaces[i] = ResetSlots(_workspaces[i]);
					SwapDown(i);
					return;
			}
			switch (Compare(_workspaces[l], _workspaces[i]))
			{
				case int n when n < 0:
					(_workspaces[i], _workspaces[l], i) = (_workspaces[l], _workspaces[i], l);
					SwapDown(l);
					break;
				case 0:
					bool areEqual = (_workspaces[i]!.Equals(_workspaces[l]));
					if (areEqual)
					{
						// They are equal! what do we do?
						RemoveElementAt(l);
						_workspaces[i] = ResetSlots(_workspaces[i]);
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

		private WorkspaceNode? Pop()
		{

			var result = _workspaces[0];
			RemoveElementAt(0);
			return result;
		}

		private void PurgeDuplicates()
		{
			Array.Sort(_workspaces, new Comparison<WorkspaceNode?>(Compare));
			int i = 0;
			for(int j=1; j < _workspaces.Length; ++j)
			{
				if(!(_workspaces[j].Equals(_workspaces[i])))
				{
					++i;
					_workspaces[i] = _workspaces[j];
				}
			}
			this._length = i+1;
		}

		internal void RemoveItems(Func<WorkspaceNode, bool> pred)
		{
			for(int i = this._length-1; i >= 0; --i)
			{
				if(pred(_workspaces[i]))
				{
					this.RemoveElementAt(i);
				}
			}
		}

		private void Push(WorkspaceNode workspace)
		{
			if (_length == _workspaces.Length)
			{
				//PurgeDuplicates();
			}
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

		private WorkspaceNode SwapElement(WorkspaceNode workspaceNode)
		{
			if(this.IsEmpty || Compare(workspaceNode, _workspaces[0]) < 0)
			{
				return workspaceNode;
			}
			else
			{
				var rv = _workspaces[0];
				_workspaces[0] = workspaceNode;
				SwapDown(0);
				return rv;
			}
		}

		public IEnumerable<WorkspaceNode> Swap(IEnumerable<WorkspaceNode> workspaceNodes, int maxReturnCount)
		{
			List<WorkspaceNode> rv = new List<WorkspaceNode>();
			foreach(var workspaceNode in workspaceNodes)
			{
				if(rv.Count < maxReturnCount)
				{
					rv.Add(this.SwapElement(workspaceNode));
				}
				else
				{
					Push(workspaceNode);
				}
			}
			if(maxReturnCount - rv.Count >= this._length)
			{
				for(int i = 0; i < this._length; ++i)
				{
					rv.Add(this._workspaces[i]);
				}
				this._length = 0;
			}
			else
			{
				while(!IsEmpty && (rv.Count < maxReturnCount))
				{
					var value = this.Pop();
					if (value != null)
					{
						rv.Add(value);
					}
					else
					{
						return rv;
					}
				}
			}
			return rv;
		}

		public bool IsEmpty => _length == 0;
	}
}
