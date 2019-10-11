using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace crozzle_desktop
{
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

		public bool IsRunning { get; set; }

		public event EventHandler EngineStarted;

		public event EventHandler EngineStopped;

		public void FireEngineStarted()
		{
			EngineStarted?.Invoke(this, EventArgs.Empty);
		}

		public void FireEngineStopped()
		{
			EngineStopped?.Invoke(this, EventArgs.Empty);
		}

		public void Start()
		{

		}
	}
}
