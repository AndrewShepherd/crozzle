using crozzle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;

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
				FirePropertyChangedEvents(
					nameof(Words),
					nameof(CanToggleOnOff),
					nameof(ToggleStartStopCommandText)
				);
			}
		}


		public Workspace BestSolution
		{
			get => _bestWorkspace;
			set => _bestWorkspace = value;
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

		private readonly DelegateCommand _toggleOnOffCommand;

		public ICommand ToggleOnOffCommand
		{
			get
			{
				return new DelegateCommand
				(
					() => this.ToggleStartStop(),
					() => this.CanToggleOnOff
				);
			}
		}

		private void ToggleStartStop()
		{
			if(this.Engine.IsRunning)
			{
			}
			else
			{
				this.StartEngine();
			}
			FirePropertyChangedEvents(nameof(ToggleStartStopCommandText));
		}

		public string ToggleStartStopCommandText =>
			(CanToggleOnOff && this.Engine.IsRunning) ? "Stop" : "Start";

		public bool CanToggleOnOff => this.Engine?.Words != null;

		private void StartEngine()
		{
			Engine._cancellationTokenSource?.Cancel();
			Engine._cancellationTokenSource = new CancellationTokenSource();
			Speedometer.Measure(Engine);
			_stopWatch.Start();
			Engine.Start();
		}


	}
}
