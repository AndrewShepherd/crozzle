using crozzle;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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

		}
	}
}
