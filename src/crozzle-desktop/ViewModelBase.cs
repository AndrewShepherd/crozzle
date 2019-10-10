using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace crozzle_desktop
{
	class ViewModelBase : INotifyPropertyChanged
	{
		private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

		public event PropertyChangedEventHandler PropertyChanged;


		private ImmutableHashSet<String> _propertyNamesToFire = ImmutableHashSet<string>.Empty;
		protected void FirePropertyChangedEvents(params string[] propertyNames)
		{
			foreach (var n in propertyNames)
			{
				Interlocked.Exchange(
					ref _propertyNamesToFire,
					_propertyNamesToFire.Add(n)
				);
			}
			this._dispatcher.BeginInvoke(
				() =>
				{
					var pn = Interlocked.Exchange(
						ref _propertyNamesToFire,
						ImmutableHashSet<string>.Empty
					);
					foreach (var n in pn)
					{
						PropertyChanged?.Invoke(
							this,
							new PropertyChangedEventArgs(n)
						);
					}
				}
			);
		}
	}
}
