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
	sealed class MainWindowViewModel : PropertyChangedEventSource
	{
		private Workspace _bestWorkspace;

		public IEnumerable<string> Words
		{
			get => Engine.Words;
			set
			{
				Engine.Words = value?.ToList();
				FirePropertyChangedEvents(nameof(Words));
				if(this.Engine.Words != null)
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
		
		public ulong GeneratedSolutionCount
		{
			get => Engine?.SolutionsGenerated ?? 0;
			set
			{
				if (Engine != null)
				{
					Engine.SolutionsGenerated = value;
				}
			}
		}

		readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.1);

		public Speedometer Speedometer { get; } = new Speedometer();

		void IncrementSolutionCount()
		{
			++this.Engine.SolutionsGenerated;
		}

		public Engine Engine { get; } = new Engine();


		public TimeSpan? RunDuration
		{
			get => _startDateTime.HasValue ? DateTime.Now - _startDateTime : null;
		}

		DateTime? _startDateTime;
		private void StartTimer()
		{
			_startDateTime = DateTime.Now;
			var timer = new System.Timers.Timer(1000);
			timer.Elapsed += (sender, args) => FirePropertyChangedEvents(nameof(RunDuration));
			timer.Start();
		}

		private StopWatch _stopWatch = new StopWatch();
		public StopWatch StopWatch
		{
			get => _stopWatch;
			set
			{
				if(_stopWatch != value)
				{
					_stopWatch = value;
					FirePropertyChangedEvents(nameof(StopWatch));
				}
			}
		}

		private void StartEngine()
		{
			Engine._cancellationTokenSource?.Cancel();
			Engine._cancellationTokenSource = new CancellationTokenSource();
			Workspace workspace = Workspace.Generate(this.Words);
			var workspaces = this.Words
				.Select(w => workspace.PlaceWord(Direction.Across, w, 0, 0))
				.ToArray();
			this.GeneratedSolutionCount = 0;
			StartTimer();


			Task.Factory.StartNew(
				() =>
				{
					Speedometer.Measure(Engine);
					_stopWatch.Start();
					DateTime lastRefresh = DateTime.UtcNow;
					int maxScore = 0;
					Engine.FireEngineStarted();
					foreach (var thisWorkspace in crozzle.Runner.SolveUsingQueue(
						workspaces,
						10000000,
						8192,
						Engine._cancellationTokenSource.Token
					))
					{
						IncrementSolutionCount();
						DateTime now = DateTime.UtcNow;
						if(now - lastRefresh > RefreshInterval)
						{
							this.Engine.LastSolution = thisWorkspace;
							lastRefresh = DateTime.UtcNow;
						}

						if (thisWorkspace.Score > maxScore)
						{
							maxScore = thisWorkspace.Score;
							this._bestWorkspace = thisWorkspace;
							this.BestScore = String.Format("Scored {0:N0} at {1:N0} solutions", maxScore, this.GeneratedSolutionCount);
							FirePropertyChangedEvents(
								nameof(BestSolution),
								nameof(BestScore)
							);
						}
					}
					Engine.FireEngineStopped();
				}
			);
		}
	}
}
