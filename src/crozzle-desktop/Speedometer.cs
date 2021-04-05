using crozzle_controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace crozzle_desktop
{
	interface ISolutionEngine
	{
		event EventHandler EngineStarted;
		event EventHandler EngineStopped;
		ulong SolutionsGenerated { get; }
		bool IsRunning { get; }
	}
 
	class Speedometer : PropertyChangedEventSource
	{
		class Entry
		{
			internal DateTime DateTime { get; private set; } = DateTime.Now;
			internal ulong SolutionCount { get; set; }
		}

		private Queue<Entry> _measurements = new Queue<Entry>();

		private ISolutionEngine _lastSolutionEngine = null;
		public void Measure(ISolutionEngine engine)
		{
			ISolutionEngine lastEngine = Interlocked.Exchange(ref _lastSolutionEngine, engine);
			if(lastEngine != null)
			{
				lastEngine.EngineStarted -= EngineStarted;
				lastEngine.EngineStopped -= EngineStopped;
			}
			if(engine != null)
			{
				engine.EngineStarted += EngineStarted;
				engine.EngineStopped += EngineStopped;
			}
			if(engine.IsRunning)
			{
				BeginMeasuring();
			}
		}

		public double? SolutionsPerSecond { get; private set; }

		private object _syncLock = new object();

		System.Timers.Timer _timer = null;
		void BeginMeasuring()
		{
			lock(_syncLock)
			{
				_measurements.Clear();
				_measurements.Enqueue(
					new Entry
					{
						SolutionCount = _lastSolutionEngine.SolutionsGenerated
					}
				);
				_timer = new System.Timers.Timer(1000);
				_timer.Elapsed += TimerElapsed;
				_timer.Start();
			}
		}

		readonly TimeSpan _extrapolationDuration = TimeSpan.FromSeconds(5.0);

		private void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			lock(_syncLock)
			{
				var entry = new Entry
				{
					SolutionCount = _lastSolutionEngine.SolutionsGenerated
				};
				Entry firstEntry = null;
				while(
					_measurements.TryPeek(out firstEntry)
					&& ((entry.DateTime - firstEntry.DateTime) > _extrapolationDuration)
				)
				{
					_measurements.TryDequeue(out firstEntry);
				}
				if(firstEntry != null)
				{
					double totalSeconds = (entry.DateTime - firstEntry.DateTime).TotalSeconds;
					this.SolutionsPerSecond = (entry.SolutionCount - firstEntry.SolutionCount) / totalSeconds;
				}
				else
				{
					this.SolutionsPerSecond = null;
				}
				_measurements.Enqueue(entry);
				FirePropertyChangedEvents(nameof(SolutionsPerSecond));
			}
		}

		void StopMeasuring()
		{
			lock(_syncLock)
			{
				this.SolutionsPerSecond = null;
				_timer?.Stop();
				_timer?.Dispose();
				_timer = null;
				_measurements.Clear();
			}
		}

		void EngineStarted(object sender, EventArgs args)
		{
			BeginMeasuring();
		}

		void EngineStopped(object sender, EventArgs args)
		{
			StopMeasuring();
		}
	}
}
