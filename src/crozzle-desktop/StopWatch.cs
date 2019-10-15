using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle_desktop
{
	public sealed class StopWatch
	{
		private TimeSpan _priorElapsedTime = TimeSpan.Zero;

		private DateTime? _startedTime;

		public event EventHandler Started;
		public event EventHandler Stopped;
		public event EventHandler Resetted; 

		public void Start()
		{
			_startedTime = DateTime.Now;
			Started?.Invoke(this, EventArgs.Empty);
		}

		public void Stop()
		{
			_priorElapsedTime = Elapsed;
			_startedTime = null;
			Stopped?.Invoke(this, EventArgs.Empty);
		}

		public void Reset()
		{
			_priorElapsedTime = TimeSpan.Zero;
			_startedTime = null;
			Resetted?.Invoke(this, EventArgs.Empty);
		}

		public TimeSpan Elapsed =>
			_priorElapsedTime
			+ (
				_startedTime.HasValue
					? DateTime.Now - _startedTime.Value
					: TimeSpan.Zero
			);
	}
}
