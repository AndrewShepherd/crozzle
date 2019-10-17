using crozzle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace crozzle_desktop
{
	class SolutionGeneratedEventArgs : EventArgs
	{
		public ulong SolutionNumber { get; set; }
		public Workspace Solution { get; set; }
	}

	class Engine : PropertyChangedEventSource, ISolutionEngine
	{
		public CancellationTokenSource _cancellationTokenSource;

		public IEnumerable<string> Words { get; set; }

		private ulong _solutionsGenerated;
		public ulong SolutionsGenerated 
		{
			get => _solutionsGenerated;
			set
			{
				if(_solutionsGenerated != value)
				{
					_solutionsGenerated = value;
					base.FirePropertyChangedEvents(nameof(SolutionsGenerated));
				}
			}
		}

		private Workspace _lastSolution;

		public Workspace LastSolution
		{
			get => _lastSolution;
			set
			{
				Interlocked.Exchange(ref _lastSolution, value);
				base.FirePropertyChangedEvents(nameof(LastSolution));
			}
		}


		public bool IsRunning { get; set; }

		public event EventHandler EngineStarted;

		public event EventHandler EngineStopped;

		public event EventHandler<SolutionGeneratedEventArgs> SolutionGenerated;

		public void FireEngineStarted()
		{
			EngineStarted?.Invoke(this, EventArgs.Empty);
		}

		public void FireEngineStopped()
		{
			EngineStopped?.Invoke(this, EventArgs.Empty);
		}

		public void FireSolutionGenerated(ulong solutionNumber, Workspace solution)
		{
			SolutionGenerated?.Invoke(
				this,
				new SolutionGeneratedEventArgs
				{
					Solution = solution,
					SolutionNumber = solutionNumber
				}
			);
		}


		public void Start()
		{
			Workspace workspace = Workspace.Generate(this.Words);
			var workspaces = this.Words
				.Select(w => workspace.PlaceWord(Direction.Across, w, 0, 0))
				.ToArray();
			this.SolutionsGenerated = 0;
			this.IsRunning = true;

			Task.Factory.StartNew(
				() =>
				{
					this.FireEngineStarted();
					foreach (var thisWorkspace in crozzle.Runner.SolveUsingQueue(
						workspaces,
						10000000,
						8192,
						this._cancellationTokenSource.Token
					))
					{
						++this.SolutionsGenerated;
						this.LastSolution = thisWorkspace;
						this.FireSolutionGenerated(
							this.SolutionsGenerated,
							thisWorkspace
						);
					}
					this.FireEngineStopped();
				}
			);

		}
	}
}
