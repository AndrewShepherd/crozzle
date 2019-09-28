using crozzle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace crozzle_desktop
{
	class MainWindowViewModel : INotifyPropertyChanged
	{
		private List<string> _words;
		private Workspace _bestWorkspace;
		private CancellationTokenSource _cancellationTokenSource;

		private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
		public List<string> Words
		{
			get => _words;
			set
			{
				this._words = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Words)));
				StartEngine();
			}
		}


		public string BestSolution
		{
			get => _bestWorkspace?.BoardRepresentation;
		}

		public Workspace _lastWorkspace;

		public string LastSolution
		{
			get => _lastWorkspace?.BoardRepresentation;
		}

		public ulong GeneratedSolutionCount { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;


		readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.25);
		private void StartEngine()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource = new CancellationTokenSource();
			Workspace workspace = Workspace.Generate(this.Words);
			var workspaces = this.Words
				.Select(w => workspace.PlaceWord(Direction.Across, w, 0, 0))
				.ToArray();
			this.GeneratedSolutionCount = 0;
			Task.Factory.StartNew(
				() =>
				{
					DateTime lastRefresh = DateTime.UtcNow;
					int maxScore = 0;
					foreach (var thisWorkspace in Runner.SolveUsingQueue(
						workspaces,
						10000000,
						256,
						_cancellationTokenSource.Token
					))
					{
						++(this.GeneratedSolutionCount);
						if(DateTime.UtcNow - lastRefresh > RefreshInterval)
						{
							this._lastWorkspace = thisWorkspace;

							_dispatcher.BeginInvoke(
								() =>
								{
									foreach(var propertyName in new[] { 
										nameof(LastSolution),
										nameof(GeneratedSolutionCount)
									})
									{
										this
											.PropertyChanged
											?.Invoke(
												this,
												new PropertyChangedEventArgs(propertyName)
											);
									}
								}
							);
							lastRefresh = DateTime.UtcNow;
						}

						if (thisWorkspace.Score > maxScore)
						{
							maxScore = thisWorkspace.Score;
							this._bestWorkspace = thisWorkspace;

							_dispatcher.BeginInvoke(
								() => 
									this
										.PropertyChanged
										?.Invoke(
											this,
											new PropertyChangedEventArgs(nameof(BestSolution))
										)
							);
						}
					}
				}
			);
		}
	}
}
