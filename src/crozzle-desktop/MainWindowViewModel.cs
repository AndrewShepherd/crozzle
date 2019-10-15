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

		public MainWindowViewModel()
		{
			this.Engine = new Engine();
		}

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

		int _maxScore = 0;
		private Engine _engine;
		public Engine Engine 
		{
			get => _engine;
			set
			{
				_engine = value;
				_engine.SolutionGenerated += this.SolutionGenerated;
			}
		}

		private void SolutionGenerated(object sender, SolutionGeneratedEventArgs e)
		{
			if (e.Solution.Score > _maxScore)
			{
				_maxScore = e.Solution.Score;

				this._bestWorkspace = e.Solution;
				this.BestScore = String.Format(
					"Scored {0:N0} at {1:N0} solutions",
					_maxScore, 
					e.SolutionNumber
				);
				FirePropertyChangedEvents(
					nameof(BestSolution),
					nameof(BestScore)
				);
			}
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
			this.Engine.SolutionsGenerated = 0;

			Task.Factory.StartNew(
				() =>
				{
					Speedometer.Measure(Engine);
					_stopWatch.Start();
					Engine.FireEngineStarted();
					foreach (var thisWorkspace in crozzle.Runner.SolveUsingQueue(
						workspaces,
						10000000,
						8192,
						Engine._cancellationTokenSource.Token
					))
					{
						++this.Engine.SolutionsGenerated;
						this.Engine.LastSolution = thisWorkspace;
						Engine.FireSolutionGenerated(
							this.Engine.SolutionsGenerated,
							thisWorkspace
						);
					}
					Engine.FireEngineStopped();
				}
			);
		}
	}
}
