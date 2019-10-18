using crozzle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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


		public ICommand ToggleOnOffCommand =>
			new DelegateCommand(
				this.ToggleStartStop,
				() => this.CanToggleOnOff
			);

		public ICommand ResetCommand =>
			new DelegateCommand(
				this.Reset,
				() => this.CanToggleOnOff
			);

		private async void ToggleStartStop()
		{
			if(this.Engine.IsRunning)
			{
				this.Engine?.Pause();
				_stopWatch.Stop();
			}
			else
			{
				await this.StartEngine();
			}
			FirePropertyChangedEvents(nameof(ToggleStartStopCommandText));
		}

		private async void Reset()
		{
			await this.Engine?.Reset();
			_stopWatch.Reset();
			FirePropertyChangedEvents(nameof(ToggleStartStopCommandText));
		}

		public string ToggleStartStopCommandText =>
			(CanToggleOnOff && this.Engine.IsRunning) ? "Pause" : "Start";

		public bool CanToggleOnOff => this.Engine?.Words != null;

		private async Task StartEngine()
		{
			Speedometer.Measure(Engine);
			_stopWatch.Start();
			await Engine.Start();
		}


	}
}
