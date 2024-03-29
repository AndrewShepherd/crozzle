﻿using crozzle;
using crozzle_controls;
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
		private CancellationTokenSource _cancellationTokenSource;

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


		public bool IsRunning => this._state == EngineState.Running;

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

		private readonly ManualResetEvent _continuation = new ManualResetEvent(true);
		public void Pause()
		{
			EngineStopped?.Invoke(this, EventArgs.Empty);
			_continuation.Reset();
			this._state = EngineState.Paused;
		}

		public async Task Reset()
		{
			this._cancellationTokenSource?.Cancel();
			_continuation.Set();
			await _currentlyRunningTask;
			LastSolution = null;
			SolutionsGenerated = 0;
			this._state = EngineState.Reset;
		}

		private enum EngineState { Reset, Running, Paused };
		private EngineState _state = EngineState.Reset;


		private void Resume()
		{
			this._continuation.Set();
			this._state = EngineState.Running;
			this.FireEngineStarted();
		}

		Task _currentlyRunningTask = Task.FromResult(0);

		public INextStepGenerator NextStepGenerator { get; set; }
		public PositioningBehavior PositioningBehavior { get; internal set; }

		private IEnumerable<Workspace> GenerateSeedWorkspaces()
		{
			Workspace workspace = Workspace.Generate(this.Words);
			if (this.PositioningBehavior == PositioningBehavior.Fixed)
			{
				workspace = workspace.ExpandSize(
					new Rectangle(
						new Location(-1, -1),
						Board.MaxWidth,
						Board.MaxHeight)
					);
			}
			var bestWord = this.Words
				.OrderByDescending(w => Scoring.Score(w))
				.First();
			var workspaces = this.Words
				.Select(word => workspace.PlaceWord(Direction.Across, word, 0, 0))
				.ToArray();
			return workspaces;
		}

		private async Task Restart()
		{
			await this.Reset();
			this._state = EngineState.Running;


			this._cancellationTokenSource = new CancellationTokenSource();

			var workspaces = GenerateSeedWorkspaces();
			_currentlyRunningTask = Task.Factory.StartNew(
				() =>
				{
					this.FireEngineStarted();
					foreach (var thisWorkspace in crozzle.Runner.SolveUsingQueue(
						workspaces,
						2000000, // Queue size
						128, // 1028, // Beam size,
						this.NextStepGenerator,
						this._cancellationTokenSource.Token
					))
					{
						++this.SolutionsGenerated;
						this.LastSolution = thisWorkspace;
						this.FireSolutionGenerated(
							this.SolutionsGenerated,
							thisWorkspace
						);
						_continuation.WaitOne();
						if(this._cancellationTokenSource.Token.IsCancellationRequested)
						{
							break;
						}
					}
					this.FireEngineStopped();
				}
			);

		}

		public async Task Start()
		{
			if(_state == EngineState.Paused)
			{
				Resume();
			}
			else
			{
				await Restart();
			}
		}
	}
}
