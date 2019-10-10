using crozzle;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace crozzle_desktop
{
	sealed class MainWindowViewModel : ViewModelBase
	{
		private List<string> _words;
		private Workspace _bestWorkspace;
		private CancellationTokenSource _cancellationTokenSource;

		public IEnumerable<string> Words
		{
			get => _words;
			set
			{
				this._words = value?.ToList();
				FirePropertyChangedEvents(nameof(Words));
				if(this._words != null)
				{
					StartEngine();
				}
			}
		}


		public Workspace BestSolution
		{
			get => _bestWorkspace;
		}

		public string BestScore
		{
			get;
			set;
		}
		


		public Workspace _lastWorkspace;

		public Workspace LastSolution
		{
			get => _lastWorkspace;
		}

		public ulong GeneratedSolutionCount { get; set; }


		readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.1);


		private int[] SolutionsEachSecond = new int[10];

		public double SolutionsPerSecond
		{
			get
			{
				if(_startingSecond == -1)
				{
					return default(double);
				}
				int sum = 0;
				int secondNow = DateTime.Now.Second % SolutionsEachSecond.Length;
				for(int i = _startingSecond; i < SolutionsEachSecond.Length; ++i)
				{
					if(i != secondNow)
					{
						sum += SolutionsEachSecond[i];
					}
				}
				return sum / (SolutionsEachSecond.Length-1);
			}
		}

		int _lastSecondChecked = -1;
		int _startingSecond = -1;
		void IncrementSolutionCount()
		{
			int thisSecond = DateTime.Now.Second % SolutionsEachSecond.Length;
			_startingSecond = Math.Min(_startingSecond, thisSecond);
			if (_lastSecondChecked != thisSecond)
			{
				Interlocked.Exchange(ref SolutionsEachSecond[thisSecond], 0);
			}

			_lastSecondChecked = thisSecond;
			Interlocked.Increment(ref SolutionsEachSecond[thisSecond]);
			++this.GeneratedSolutionCount;
		}

		private void StartEngine()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource = new CancellationTokenSource();
			Workspace workspace = Workspace.Generate(this.Words);
			var workspaces = this.Words
				.Select(w => workspace.PlaceWord(Direction.Across, w, 0, 0))
				.ToArray();
			this.GeneratedSolutionCount = 0;
			

			Task.Factory.StartNew(
				() =>
				{
					DateTime lastRefresh = DateTime.UtcNow;
					_startingSecond = lastRefresh.Second;
					int maxScore = 0;
					foreach (var thisWorkspace in Runner.SolveUsingQueue(
						workspaces,
						10000000,
						4096,
						_cancellationTokenSource.Token
					))
					{
						IncrementSolutionCount();
						DateTime now = DateTime.UtcNow;
						if(now - lastRefresh > RefreshInterval)
						{
							this._lastWorkspace = thisWorkspace;
							FirePropertyChangedEvents(
								nameof(LastSolution),
								nameof(GeneratedSolutionCount),
								nameof(SolutionsPerSecond)
							);
							lastRefresh = DateTime.UtcNow;
						}

						if (thisWorkspace.Score > maxScore)
						{
							maxScore = thisWorkspace.Score;
							this._bestWorkspace = thisWorkspace;
							this.BestScore = String.Format("Scored {0:N0} at {1:N0} solutions", maxScore, this.GeneratedSolutionCount);
							FirePropertyChangedEvents(nameof(BestSolution), nameof(BestScore));
						}
					}
				}
			);
		}
	}
}
