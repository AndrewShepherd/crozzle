using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace crozzle_desktop
{
	/// <summary>
	/// Interaction logic for StopWatchView.xaml
	/// </summary>
	public partial class StopWatchView : UserControl
	{
		public static DependencyProperty StopWatchDependencyProperty = DependencyProperty.Register(
			nameof(StopWatch),
			typeof(StopWatch),
			typeof(StopWatchView),
			new PropertyMetadata
			{ 
				PropertyChangedCallback = (d, e) =>
				{
					var stopWatchView = d as StopWatchView;
					var stopWatch = e.NewValue as StopWatch;
					if(stopWatchView != null)
					{
						stopWatchView.StopWatch = stopWatch;
					}
				}
			}

		);
		public StopWatchView()
		{
			InitializeComponent();
		}

		private StopWatch _stopWatch = null;

		public StopWatch StopWatch
		{
			get => _stopWatch;
			set
			{
				var previousStopWatch = Interlocked.Exchange(ref _stopWatch, value);
				if(previousStopWatch != null)
				{
					previousStopWatch.Started -= StopWatchStarted;
					previousStopWatch.Stopped -= StopWatchStopped;
					previousStopWatch.Resetted -= StopWatchResetted;
				}
				_stopWatch.Started += StopWatchStarted;
				_stopWatch.Stopped += StopWatchStopped;
				_stopWatch.Resetted += StopWatchResetted;
			}
		}

		void DisplayTimeSpan(TimeSpan? timeSpan)
		{
			var text = timeSpan.HasValue
				? string.Format(
					"{0:0;;#} {1:00}:{2:00;00}:{3:00;00}",
					Math.Floor(timeSpan.Value.TotalDays),
					timeSpan.Value.Hours,
					timeSpan.Value.Minutes,
					timeSpan.Value.Seconds
				)
				: string.Empty;
			this.Dispatcher.BeginInvoke(
				new Action(
					() =>
					{
						this.TimeSpanTextBlock.Text = text;
					}
				)
			);
		}

		Timer _timer = null;

		void TimerElapsed(object sender)
		{
			DisplayTimeSpan(_stopWatch?.Elapsed);
		}

		async Task StartTimer()
		{
			await ClearTimer();
			var timeSpan = _stopWatch?.Elapsed;
			int millisecondsUntilNextSecond = 1000;
			if(timeSpan.HasValue)
			{
				millisecondsUntilNextSecond -= timeSpan.Value.Milliseconds;
			}
			_timer = new Timer(TimerElapsed, null, millisecondsUntilNextSecond, 1000);
		}

		async Task ClearTimer()
		{
			if(_timer != null)
			{
				await _timer.DisposeAsync();
				_timer = null;
			}
		}

		private async void StopWatchResetted(object sender, EventArgs e)
		{
			DisplayTimeSpan(null);
			await ClearTimer();
		}

		private async void StopWatchStopped(object sender, EventArgs e)
		{
			DisplayTimeSpan(_stopWatch?.Elapsed);
			await ClearTimer();
		}

		private async void StopWatchStarted(object sender, EventArgs e)
		{
			DisplayTimeSpan(_stopWatch?.Elapsed);
			await StartTimer();
		}
	}
}
