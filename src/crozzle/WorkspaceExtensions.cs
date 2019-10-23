namespace crozzle
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;

	public static class WorkspaceExtensions
	{
		public static Workspace Clone(this Workspace workspace) =>
			new Workspace
			{
				Score = workspace.Score,
				Board = workspace.Board,
				WordDatabase = workspace.WordDatabase,
				Slots = workspace.Slots,
				IsValid = workspace.IsValid,
				IncludedWords = workspace.IncludedWords,
				Intersections = workspace.Intersections
			};

		public static Workspace PopSlot(this Workspace workspace, out Slot slot)
		{
			if (workspace.Slots.IsEmpty)
			{
				slot = null;
				return workspace;
			}
			else
			{
				slot = workspace.Slots.OrderByDescending(s => Scoring.Score(s.Letter)).First();
				return workspace.RemoveSlot(slot);
			}
		}

		public static Workspace RemoveSlot(this Workspace workspace, Slot slot)
		{
			var clone = workspace.Clone();
			clone.Slots = clone.Slots.Remove(slot);
			return clone;
		}

		public static Workspace RemoveWord(this Workspace workspace, string word)
		{
			var newWorkspace = WorkspaceExtensions.Clone(workspace);
			newWorkspace.WordDatabase = workspace.WordDatabase.Remove(word);
			return newWorkspace;
		}

		public static Workspace ExpandSize(this Workspace workspace, Rectangle newRectangle)
		{
			var newWorkspace = workspace.Clone();
			newWorkspace.Board = workspace.Board.ExpandSize(newRectangle);
			return newWorkspace;
		}

		public static Rectangle GetCurrentRectangle(this Workspace workspace) => workspace.Board.Rectangle;

		public static Workspace PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y) =>
			TryPlaceWord(
				workspace,
				workspace.GenerateGrid(),
				new WordPlacement(
					direction,
					new Location(x, y),
					word
				)
			);

		private static bool RectangleIsTooBig(Rectangle rectangle)
		{ 
			if(rectangle.Width > Board.MaxWidth)
			{
				return true;
			}
			return (rectangle.Height > Board.MaxHeight);
		}

		// Places the word in the workspace
		// 
		// The new workspace will have
		//  - Different Slots
		//  - Different Partial Words
		//  - Different wordplacements
		//  - Different available words
		//  - Different Score
		internal static Workspace TryPlaceWord(this Workspace workspace, Grid grid, WordPlacement wordPlacement)
		{
			var rectangle = wordPlacement.GetRectangle();		
			var newWorkspace = workspace.ExpandSize(
				rectangle
			);
			if(RectangleIsTooBig(newWorkspace.Board.Rectangle))
			{
				return null;
			}
			newWorkspace.IsValid = true;
			newWorkspace.WordDatabase = newWorkspace.WordDatabase.Remove(wordPlacement.Word);
			newWorkspace.IncludedWords = newWorkspace.IncludedWords.Add(wordPlacement.Word);
			newWorkspace.Score = workspace.Score + Scoring.ScorePerWord;
			int advanceIncrement = wordPlacement.Direction == Direction.Across
				? 1
				: newWorkspace.Board.Rectangle.Width;
			int x = wordPlacement.Location.X;
			int y = wordPlacement.Location.Y;

			Vector locationIncrement = wordPlacement.Direction == Direction.Across
				? new Vector(1, 0)
				: new Vector(0, 1);
			for(
					(int stringIndex, Location l) = (0, wordPlacement.Location);
					stringIndex < wordPlacement.Word.Length;
					++stringIndex, l = l+locationIncrement
				)
			{
				var gridCell = grid.CellAt(l);
				if(gridCell.CellType == GridCellType.AvailableSlot)
				{
					newWorkspace.Score += Scoring.Score(gridCell.Slot.Letter);
					newWorkspace.Intersections = newWorkspace.Intersections.Add(
						new Intersection
						{
							Word = wordPlacement.Word,
							Index = stringIndex
						}
					);
					newWorkspace.Slots = newWorkspace.Slots.Remove(gridCell.Slot);
				}
				else if (gridCell.CellType == GridCellType.Blank)
				{
					newWorkspace.Slots = newWorkspace.Slots.Add(
						new Slot(
							wordPlacement.Word[stringIndex],
							wordPlacement.Direction == Direction.Across ? Direction.Down : Direction.Across,
							l
						)
					);
					bool formsPartialWord = wordPlacement.Direction == Direction.Across
						? ((gridCell.PartialWordAbove != null) || (gridCell.PartialWordBelow != null))
						: ((gridCell.PartialWordToLeft != null) || (gridCell.PartialWordToRight != null));
					if(formsPartialWord)
					{
						newWorkspace.IsValid = false;
						// We test that there is a hope that the partial word 
						// can be filled
						if(true)
						{
							var partialWord = gridCell.PredictPartialWordToBeCreated(
								l,
								wordPlacement.Word[stringIndex],
								wordPlacement.Direction
							);
							if(!(newWorkspace.WordDatabase.CanMatchWord(partialWord.Value)))
							{
								return null;
							}
						}
					}

				}
				else
				{
					return null;
					throw new Exception("Unexpected state");
				}
			}
			newWorkspace.Board = newWorkspace.Board.PlaceWord(
				wordPlacement
			);
			return newWorkspace;			
		}

		internal static Grid GenerateGrid(this Workspace workspace)
		{
			GridCell[] gridCells = new GridCell[workspace.Board.Rectangle.Area];
			Func<int, int> moveUp = n => n - workspace.Board.Rectangle.Width,
				moveDown = n => n + workspace.Board.Rectangle.Width,
				moveLeft = n => n - 1,
				moveRight = n => n + 1;
			for (int i = 0; i < gridCells.Length; ++i)
			{
				gridCells[i] = new GridCell();
			}
			foreach(var slot in workspace.Slots)
			{
				var cell = gridCells[workspace.Board.Rectangle.IndexOf(slot.Location)];
				cell.Slot = slot;
				cell.CellType = GridCellType.AvailableSlot;
			}
			foreach(var wordPlacement in workspace.Board.WordPlacements)
			{
				(Func<int, int> forward, Func<int, int> back) =
					wordPlacement.Direction == Direction.Across
					? (moveRight, moveLeft)
					: (moveDown, moveUp);
				int gridLocation = workspace.Board.Rectangle.IndexOf(wordPlacement.Location);
				if (gridCells[back(gridLocation)].CellType == GridCellType.AvailableSlot)
				{
					throw new Exception("Workspace got into an invalid state");
				}
				gridCells[back(gridLocation)].CellType = GridCellType.EndOfWordMarker;
				for (
					int i = 0; i < wordPlacement.Word.Length;
					++i,
					gridLocation = forward(gridLocation)
				)
				{
					var gridCell = gridCells[gridLocation];
					gridCell.Letter = wordPlacement.Word[i];
					if(gridCell.CellType != GridCellType.AvailableSlot)
					{
						gridCell.CellType = GridCellType.Complete;
						// Must go on either side and set it to EnforcedBlank
						// if they are already blank
						(int s1, int s2) = wordPlacement.Direction == Direction.Across
							? (moveUp(gridLocation), moveDown(gridLocation))
							: (moveLeft(gridLocation), moveRight(gridLocation));
						foreach(var s in new[] { s1, s2})
						{
							if((s >= 0) && (s < gridCells.Length))
							{
								var sideCell  = gridCells[s];
								if(sideCell.CellType == GridCellType.Blank)
								{
									sideCell.CellType = GridCellType.BlankNoAdjacentSlots;
								}
							}
						}
					}
				}
				if(gridCells[gridLocation].CellType == GridCellType.AvailableSlot)
				{
					throw new Exception("Workspace got into an invalid state");
				}
				gridCells[gridLocation].CellType = GridCellType.EndOfWordMarker;
			}

			var grid = new Grid
			{
				Rectangle = workspace.Board.Rectangle,
				Cells = gridCells
			};
			HashSet<Slot> slotsToProcess = new HashSet<Slot>(workspace.Slots);

			while(slotsToProcess.Any())
			{
				var slot = slotsToProcess.First();
				var gridCell = grid.CellAt(slot.Location);
				// We have to search for adjacent words
				int probe = grid.Rectangle.IndexOf(slot.Location);
				if(!(gridCells[probe].HasLetter))
				{
					throw new Exception("Invalid grid state");
				}
				PartialWord partialWord = null;
				if (gridCell.Slot.Direction == Direction.Across)
				{
					while (gridCells[moveRight(probe)].HasLetter)
					{
						probe = moveRight(probe);
					}
					int rightNonSlotLocation = moveRight(probe);
					Location rectangleBottomRight = workspace.Board.Rectangle.CalculateLocation(probe);
					string contiguousText = string.Empty;
					Location rectangleTopLeft = rectangleBottomRight;
					while (gridCells[probe].HasLetter)
					{
						var thisSlot = gridCells[probe].Slot;
						if(thisSlot != null)
						{
							slotsToProcess.Remove(thisSlot);
						}
						contiguousText = $"{gridCells[probe].Letter}{contiguousText}";
						rectangleTopLeft = workspace.Board.Rectangle.CalculateLocation(probe);
						probe = moveLeft(probe);
					}
					partialWord = new PartialWord
					{
						Direction = Direction.Across,
						Value = contiguousText,
						Rectangle = new Rectangle(rectangleTopLeft, rectangleBottomRight)
					};
					gridCells[probe].PartialWordToRight = partialWord;
					gridCells[rightNonSlotLocation].PartialWordToLeft = partialWord;

				}
				else
				{
					while (gridCells.CellAt(moveDown(probe)).HasLetter)
					{
						probe = moveDown(probe);
					}
					int bottomNonSlotLocation = moveDown(probe);
					Location rectangleBottomRight = workspace.Board.Rectangle.CalculateLocation(probe);
					string contiguousText = string.Empty;
					Location rectangleTopLeft = rectangleBottomRight;
					while (gridCells.CellAt(probe).HasLetter)
					{
						var thisSlot = gridCells[probe].Slot;
						if(thisSlot != null)
						{
							slotsToProcess.Remove(thisSlot);
						}
						contiguousText = $"{gridCells[probe].Letter}{contiguousText}";
						rectangleTopLeft = workspace.Board.Rectangle.CalculateLocation(probe);
						probe = moveUp(probe);
					}
					partialWord = new PartialWord
					{
						Direction = Direction.Down,
						Value = contiguousText,
						Rectangle = new Rectangle(rectangleTopLeft, rectangleBottomRight)
					};
					gridCells.CellAt(probe).PartialWordBelow = partialWord;
					gridCells.CellAt(bottomNonSlotLocation).PartialWordAbove = partialWord;
				}
				if (partialWord.Value.Length > 1)
				{
					grid.PartialWords.Add(partialWord);
				}
			}
			return grid;
		}

		private static bool CanPlaceWord(Strip strip, string candidateWord, int start)
		{
			if(start < 1)
			{
				return false;
			}
			var charBefore = strip.GridCells[start - 1];
			if (
				!(
					charBefore.CanPlaceEndOfWordMarker
				)
			)
			{
				return false;
			}
			if (start + candidateWord.Length > (strip.GridCells.Length - 1))
				return false;
			var charAfter = strip.GridCells[start + candidateWord.Length];
			if (
				!(
					charAfter.CanPlaceEndOfWordMarker
				)
			)
			{
				return false;
			}
			bool success = true;
			for (
				int cIndex = 0, sIndex = start;
				cIndex < candidateWord.Length && success;
				++cIndex, ++sIndex
			)
			{
				if (strip.GridCells[sIndex].CellType == GridCellType.Blank)
				{
					continue;
				}
				if (strip.GridCells[sIndex].Slot?.Letter != candidateWord[cIndex])
				{
					success = false;
				}
			}
			return success;
		}

		private static bool CanPlaceWord(Strip strip, WordPlacement wordPlacement)
		{
			var start = wordPlacement.Direction == Direction.Across
				? wordPlacement.Location.X - strip.StartAt
				: wordPlacement.Location.Y - strip.StartAt;
			return CanPlaceWord(
				strip,
				wordPlacement.Word,
				start
			);
		}

		internal static IEnumerable<Workspace> CoverFragment(
				this Workspace workspace,
				Grid grid,
				string fragment,
				Location location,
				Direction direction
			)
		{
			// TODO: Use GetCandidateWordPlacements from above
			var candidateWordPlacements = workspace.GetCandidateWordPlacements(
				fragment,
				location,
				direction
			);
			if (!(candidateWordPlacements.Any()))
			{
				yield break;
			}
			var strip = grid.GenerateStrip(direction, location);

			foreach(var candidateWordPlacement in candidateWordPlacements)
			{
				var fullRectangle = candidateWordPlacement.GetRectangle()
					.Union(workspace.GetCurrentRectangle());
				if(RectangleIsTooBig(fullRectangle))
				{
					continue;
				}
				// Where is the start
				bool success = CanPlaceWord(strip, candidateWordPlacement);
				if (success)
				{
					var child = workspace.TryPlaceWord(
						grid,
						candidateWordPlacement
					);
					if (child != null)
					{
						yield return child;
					}
				}
			}
		}

		private static IEnumerable<Workspace> CoverSlot(this Workspace workspace, Grid grid, Slot slot) =>
			workspace.CoverFragment(
				grid,
				new string(new[] { slot.Letter }),
				slot.Location,
				slot.Direction
			);

		private static bool CanCoverSpace(
			this Workspace workspace,
			Grid grid,
			GridRegion space
		) =>
			GetAdjacentSlots(workspace.Slots, space)
			.SelectMany(
				adj =>
					workspace.CoverSlot(grid, adj)
			).Any();

		private static bool CanCoverEachSpace(
			this Workspace workspace,
			Grid grid,
			IEnumerable<GridRegion> regions
		) =>
			regions.All(region => workspace.CanCoverSpace(grid, region));


		class CoverageConstraint
		{
			private readonly int _maxLocations = 4;

			public CoverageConstraint(int maxLocations)
			{
				_maxLocations = maxLocations;
			}

			public bool SatisfiesConstraint(GridRegion gridRegion) =>
				gridRegion.CountLocations() <= _maxLocations;
		}
		
		private static IEnumerable<Workspace> CoverOnePartialWord(Workspace workspace, Grid grid)
		{
			var partialSlot = grid.PartialWords.First();
			grid.PartialWords.Remove(partialSlot);
			workspace = workspace.Clone();
			foreach (
				var solution in CoverFragment(
					workspace,
					grid,
					partialSlot.Value,
					partialSlot.Rectangle.TopLeft,
					partialSlot.Direction
				)
			)
			{
				if (grid.PartialWords.Any())
				{
					solution.IsValid = false;
				}
				yield return solution;
			}
		}

		internal static IEnumerable<WordPlacement> GetCandidateWordPlacements(
			this Workspace workspace,
			string fragment,
			Location location,
			Direction direction
			)
		{
			var availableWords = workspace.WordDatabase.ListAvailableMatchingWords(fragment);
			foreach (var candidateWord in availableWords)
			{
				for (
					int index = candidateWord.IndexOf(fragment, 0);
					index != -1;
					index = candidateWord.IndexOf(fragment, index + 1)
				)
				{
					Location l = direction == Direction.Across
						? new Location(location.X - index, location.Y)
						: new Location(location.X, location.Y - index);
					yield return new WordPlacement
					(
						direction,
						l,
						candidateWord
					);
				}
			}
		}

		internal static IEnumerable<WordPlacement> GetCandidateWordPlacements(
			this Workspace workspace,
			Slot slot
		) =>
			GetCandidateWordPlacements(
				workspace,
				slot.Letter.ToString(),
				slot.Location,
				slot.Direction
			);

		public static bool TryCombineWorkspaces(Workspace w1, Workspace w2, out Workspace combined)
		{
			var newRectangle = w2.GetCurrentRectangle().Union(w1.GetCurrentRectangle());
			if (RectangleIsTooBig(newRectangle))
			{
				combined = null;
				return false;
			}
			combined = null;

			var justInW1 = w1.Board.WordPlacements.Except(w2.Board.WordPlacements);
			var justInW2 = w2.Board.WordPlacements.Except(w1.Board.WordPlacements);
			if (
				justInW2
					.Where(
						w2wp =>
							(
								justInW1
									.Where(
										w1wp =>
										{
											if (w1wp.Word == w2wp.Word)
												return true;
											if(
												(w1wp.Direction == w2wp.Direction)
												&& (
													w1wp.GetRectangleExcludingEndMarkers()
													.IntersectsWith(w2wp.GetRectangleExcludingEndMarkers())
												)
											)
											{
												return true;
											}
											return false;
										}
									).Any())
					).Any()
			)
			{
				return false;
			}
			foreach (var w2Placement in justInW2)
			{
				var grid = w1.GenerateGrid();
				var strip = grid.GenerateStrip(w2Placement.Direction, w2Placement.Location);
				bool canPlaceWord = CanPlaceWord(strip, w2Placement.Word, strip.SlotIndex);
				if (!canPlaceWord)
				{
					return false;
				}
				w1 = w1.TryPlaceWord(grid, w2Placement);
				if (w1 == null)
				{
					return false;
				}
			}
			combined = w1;
			var combinedGrid = combined.GenerateGrid();
			if (combinedGrid.PartialWords.Any())
			{
				combined.IsValid = false;
			}
			return true;
		}

		private static IEnumerable<Workspace> CombineGeneratedWorkspaces(IEnumerable<Workspace> g1, IEnumerable<Workspace> g2)
		{
			foreach (var w1 in g1)
			{
				foreach (var w2 in g2)
				{
					if (TryCombineWorkspaces(w1, w2, out Workspace combined))
					{
						yield return combined;
					}
				}
			}
		}

		private static IEnumerable<Workspace> CombineGeneratedWorkspaces(IEnumerable<IEnumerable<Workspace>> generators)
		{
			if (generators.Count() == 0)
			{
				return Enumerable.Empty<Workspace>();

			}
			if (generators.Count() == 1)
			{
				return generators.First();
			}
			if (!generators.All(e => e.Any()))
			{
				return Enumerable.Empty<Workspace>();
			}
			else
			{
				return CombineGeneratedWorkspaces(
					new[]
					{
						CombineGeneratedWorkspaces(
							generators.First(),
							generators.Skip(1).First()
						).Buffer()
					}.Concat(generators.Skip(2))
				);
			}
		}

		private static IEnumerable<Workspace> CoverFocusedRegion(
			this Workspace workspace,
			Grid grid,
			CoverageConstraint coverageConstraint,
			GridRegion focusedRegion,
			IImmutableSet<Slot> excludedSlots)
		{
			var slotsToFill = GetAdjacentSlots(
				workspace.Slots,
				focusedRegion // overlappingRegions.First()
			).Where(
				s => (!(excludedSlots.Contains(s)))
			).ToList();
			while (slotsToFill.Any())
			{
				var slot = slotsToFill[0];
				slotsToFill.RemoveAt(0);
				excludedSlots = excludedSlots.Add(slot);
				var candidateWordPlacements = workspace.GetCandidateWordPlacements(slot)
					.Where(
						cwp =>
						{
							var rectangle = cwp.GetRectangleExcludingEndMarkers();
							if (focusedRegion.OverlapsWith(rectangle))
							{
								return true;
							}
							else
							{
								return false;
							}

						}
					).ToList();
				if (!candidateWordPlacements.Any())
				{
					continue;
				}
				var strip = grid.GenerateStrip(slot.Direction, slot.Location);
				foreach (var candidateWordPlacement in candidateWordPlacements)
				{
					if (CanPlaceWord(strip, candidateWordPlacement))
					{
						var child = workspace.TryPlaceWord(grid, candidateWordPlacement);
						if (child != null)
						{
							foreach (var grandchild in child.CoverRegion(
								coverageConstraint,
								focusedRegion,
								excludedSlots
								)
							)
							{
								yield return grandchild;
							}
						}
					}
				}
			}

		}

		private static IEnumerable<Workspace> CoverRegion(
			this Workspace workspace,
			CoverageConstraint coverageConstraint,
			GridRegion gridRegion,
			IImmutableSet<Slot> excludedSlots
		)
		{
			var grid = workspace.GenerateGrid();
			if (grid.PartialWords.Any())
			{
				foreach(var child in CoverOnePartialWord(workspace, grid))
				{
					foreach(
						var grandchild in CoverRegion(
							child,
							coverageConstraint,
							gridRegion,
							excludedSlots
						)
					)
					{
						yield return grandchild;
					}
				}
				yield break;
			}
			// Find the overlapping regions
			var allRegions = grid.FindEnclosedSpaces(
				c =>
					(
						(c.CellType == GridCellType.Blank)
						|| (c.CellType == GridCellType.EndOfWordMarker)
						|| (c.CellType == GridCellType.BlankNoAdjacentSlots)
					)
				).Where(
					s =>
						!coverageConstraint.SatisfiesConstraint(s)
				).ToList();
			var overlappingRegions = allRegions
				.Where(r => r.OverlapsWith(gridRegion))
				.ToList();
			if (!overlappingRegions.Any())
			{
				yield return workspace;
				yield break;
			}


			var intersections = overlappingRegions
				.Select(r => r.Intersection(gridRegion))
				.Where(r => !coverageConstraint.SatisfiesConstraint(r))
				.ToList();

			if(!intersections.Any())
			{
				yield return workspace;
				yield break;
			}
			// TODO: Create a rgion that's the intersection 
			// of the gridRegion and the overlapping region
			//
			//
			// Another todo: if there are more than one overlapping region
			// we should do the divide-and-conquer trick

			var generators = intersections.Select(
				i => CoverFocusedRegion(workspace, grid, coverageConstraint, i, excludedSlots)
			);
			foreach(var child in CombineGeneratedWorkspaces(generators))
			{
				yield return child;
			}
			
		}

		public static IEnumerable<Workspace> GenerateNextSteps(this Workspace workspace)
		{
			var grid = workspace.GenerateGrid();
			if (grid.PartialWords.Any())
			{
				foreach(var child in CoverOnePartialWord(workspace, grid))
				{
					yield return child.Normalise();
				}
				yield break;
			}

			List<Slot> slotsToFill = null;
			List<GridRegion> spacesThatMustBeFilled = new List<GridRegion>();
			CoverageConstraint coverageConstraint = new CoverageConstraint(4);

			if (workspace.IncludedWords.Count() > 2)
			{
				spacesThatMustBeFilled = grid.FindEnclosedSpaces(
					c =>
						(
							(c.CellType == GridCellType.Blank)
							|| (c.CellType == GridCellType.EndOfWordMarker)
							|| (c.CellType == GridCellType.BlankNoAdjacentSlots)
						)
					).Where(
						s =>
							!coverageConstraint.SatisfiesConstraint(s)
					).ToList();
			}



			if(!(workspace.CanCoverEachSpace(grid, spacesThatMustBeFilled)))
			{
				yield break;
			}
			const int Threshold = 14; // Any larger and it runs out of memory
			if (spacesThatMustBeFilled.Any())
			{
				spacesThatMustBeFilled = spacesThatMustBeFilled
					.OrderBy(s => s.CountLocations())
				.ToList();
				IEnumerable<IEnumerable<Workspace>> generators = spacesThatMustBeFilled
					.Where(s => s.CountLocations() <= Threshold)
					.Select(
						region =>
							CoverRegion(
								workspace,
								coverageConstraint,
								region,
								ImmutableHashSet<Slot>.Empty
							).Buffer()
					).ToList();
				if(generators.Any())
				{
					foreach (var child in CombineGeneratedWorkspaces(generators))
					{
						yield return child.Normalise();
					}
					yield break;
				}
			}
			slotsToFill = workspace.Slots.ToList();

			while (slotsToFill.Any())
			{
				var slot = slotsToFill[0];
				slotsToFill.RemoveAt(0);
				workspace = workspace.RemoveSlot(slot);
				foreach(var child in workspace.CoverSlot(grid, slot))
				{
					yield return child.Normalise();
				}

				grid.RemoveSlot(slot);
			}
		}

		private static bool LocationIsAdjacentOrInside(Location l, RowIndexAndRange rr)
		{
			if (rr.RowIndex == l.Y)
			{
				var r = rr.Range;
				return (
					(l.X >= r.Start - 1)
						&& (l.X <= r.EndExclusive)
				);
			}
			if(Math.Abs(rr.RowIndex - l.Y) == 1)
			{
				var r = rr.Range;
				return (
					(l.X >= r.Start)
					&& (l.X < r.EndExclusive)
				);
			}
			return false;
		}

		private static bool LocationIsAdjacentOrInsideGridRegion(Location l, GridRegion gridRegion)
		{
			return gridRegion.RowIndexAndRanges
				.Any(
					riar => LocationIsAdjacentOrInside(l, riar)
				);
		}

		private static IEnumerable<Slot> GetAdjacentSlots(
			IEnumerable<Slot> slots,
			GridRegion gridRegion
		)
		{
			return slots.Where(s => LocationIsAdjacentOrInsideGridRegion(s.Location, gridRegion));
		}
	}
}
