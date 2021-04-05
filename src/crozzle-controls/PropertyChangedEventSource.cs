namespace crozzle_controls
{
	using System;
	using System.Collections.Concurrent;
	using System.ComponentModel;
	using System.Threading;
	using System.Windows.Threading;

	public class PropertyChangedEventSource : INotifyPropertyChanged
	{
		private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

		public event PropertyChangedEventHandler? PropertyChanged;

		private ConcurrentDictionary<String, bool> _propertyNamesToFire = new ConcurrentDictionary<string, bool>();
		private AutoResetEvent _dispatchPending = new AutoResetEvent(true);
		protected void FirePropertyChangedEvents(params string[] propertyNames)
		{
			foreach (var n in propertyNames)
			{
				_propertyNamesToFire.TryAdd(n, true);
			}
			if (_dispatchPending.WaitOne(0))
			{
				this._dispatcher.BeginInvoke(
					() =>
					{
						_dispatchPending.Set();
						var pn = Interlocked.Exchange(
							ref _propertyNamesToFire,
							new ConcurrentDictionary<string, bool>()
						);
						foreach (var n in pn)
						{
							PropertyChanged?.Invoke(
								this,
								new PropertyChangedEventArgs(n.Key)
							);
						}
					}
				);
			}

		}
	}

}
