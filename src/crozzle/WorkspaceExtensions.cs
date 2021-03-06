﻿namespace crozzle
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;

	public static class WorkspaceExtensions
	{
		public const int MaxContiguousSpaces = 6;

		public static Workspace Clone(this Workspace workspace) =>
			new Workspace
			{
				Score = workspace.Score,
				Board = workspace.Board,
				WordDatabase = workspace.WordDatabase,
				SlotEntries = workspace.SlotEntries,
				IsValid = workspace.IsValid,
				IncludedWords = workspace.IncludedWords,
				Intersections = workspace.Intersections
			};

		public static Workspace PopSlot(this Workspace workspace, out Slot? slot)
		{
			if (workspace.SlotEntries.IsEmpty)
			{
				slot = null;
				return workspace;
			}
			else
			{
				slot = workspace
					.SlotEntries
					.OrderByDescending(
						s => Scoring.Score(s.Slot.Letter)
				).Select(s => s.Slot)
				.First();
				return workspace.RemoveSlot(slot);
			}
		}

		public static Workspace RemoveSlot(this Workspace workspace, Slot slot)
		{
			var matchingSlotEntry = workspace
				.SlotEntries
				.Where(se => se.Slot.Equals(slot))
				.FirstOrDefault();
			if (matchingSlotEntry == null)
			{
				return workspace;
			}
			else
			{
				var clone = workspace.Clone();
				clone.SlotEntries = clone.SlotEntries.Remove(matchingSlotEntry);
				return clone;
			}
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

		public static Workspace? PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y) =>
			PlaceWord(
				workspace,
				new WordPlacement(
					direction,
					new Location(x, y),
					word
				)
			);

		public static Workspace? PlaceWord(this Workspace workspace, WordPlacement wordPlacement) =>
			TryPlaceWord(
				workspace,
				workspace.GenerateGrid(),
				wordPlacement
			);

		public static bool RectangleIsTooBig(Rectangle rectangle)
		{
			if (rectangle.Width > Board.MaxWidth)
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
		public static Workspace? TryPlaceWord(this Workspace workspace, Grid grid, WordPlacement wordPlacement)
		{
			var rectangle = wordPlacement.GetRectangle();
			var newWorkspace = workspace.ExpandSize(
				rectangle
			);
			if (RectangleIsTooBig(newWorkspace.Board.Rectangle))
			{
				return null;
			}
			newWorkspace.IsValid = true;
			newWorkspace.WordDatabase = newWorkspace.WordDatabase.Remove(wordPlacement.Word);
			newWorkspace.IncludedWords = newWorkspace.IncludedWords.Add(wordPlacement.Word);
			newWorkspace.Score = workspace.Score + Scoring.ScorePerWord;

			var beforeStart = grid.CellAt(
				wordPlacement.Location.Offset(
					wordPlacement.Direction,
					-1
				)
			);
			if(!beforeStart.CanPlaceEndOfWordMarker)
			{
				return null;
			}
			var afterEnd = grid.CellAt(
				wordPlacement.Location.Offset(
					wordPlacement.Direction,
					wordPlacement.Word.Length
				)
			);
			if(!afterEnd.CanPlaceEndOfWordMarker)
			{
				return null;
			}

			Vector locationIncrement = wordPlacement.Direction == Direction.Across
				? new Vector(1, 0)
				: new Vector(0, 1);
			for (
					(int stringIndex, Location l) = (0, wordPlacement.Location);
					stringIndex < wordPlacement.Word.Length;
					++stringIndex, l = l + locationIncrement
				)
			{
				var gridCell = grid.CellAt(l);
				if (
					(gridCell.CellType == GridCellType.AvailableSlot)
					&& (gridCell.Slot != null)
					&& (gridCell.WordAndIndex != null)
				)
				{
					if(gridCell.Letter != wordPlacement.Word[stringIndex])
					{
						return null;
					}
					newWorkspace.Score += Scoring.Score(gridCell.Slot.Letter);
					newWorkspace.Intersections = newWorkspace.Intersections.Add(
						new Intersection(
							new WordAndIndex(wordPlacement.Word, stringIndex),
							gridCell.WordAndIndex
						)
					);
					newWorkspace.SlotEntries = newWorkspace
						.SlotEntries
						.Remove(gridCell.Slot);
				}
				else if (gridCell.CellType == GridCellType.Blank)
				{
					var slot = new Slot(
							wordPlacement.Word[stringIndex],
							wordPlacement.Direction == Direction.Across ? Direction.Down : Direction.Across,
							l
						);
					newWorkspace.SlotEntries = newWorkspace.SlotEntries.Add(
						new SlotEntry
						(
							slot,
							null
						)
					);
					bool formsPartialWord = wordPlacement.Direction == Direction.Across
						? ((gridCell.PartialWordAbove != null) || (gridCell.PartialWordBelow != null))
						: ((gridCell.PartialWordToLeft != null) || (gridCell.PartialWordToRight != null));
					if (formsPartialWord)
					{
						newWorkspace.IsValid = false;
						// We test that there is a hope that the partial word 
						// can be filled
						if (true)
						{
							var partialWord = gridCell.PredictPartialWordToBeCreated(
								l,
								wordPlacement.Word[stringIndex],
								wordPlacement.Direction
							);
							if (!(newWorkspace.WordDatabase.CanMatchWord(partialWord.Value)))
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

		private static HashSet<Slot> GetSlotsToProcess(Workspace workspace)
		{
			var hashSet = new HashSet<Slot>(workspace.SlotEntries.Count);
			foreach(var slotEntry in workspace.SlotEntries)
			{
				if((slotEntry.CandidateWords == null) || !slotEntry.CandidateWords.IsEmpty)
				{
					hashSet.Add(slotEntry.Slot);
				}
			}
			return hashSet;
		}

		internal static Workspace ResetAllSlots(this Workspace workspace)
		{
			var result = new Workspace
			{
				WordDatabase = workspace.WordDatabase.ResetWordAvailability(),
				IncludedWords = ImmutableList<string>.Empty,
				Intersections = ImmutableList<Intersection>.Empty,
				Board = new Board
				{
					Rectangle = workspace.Board.Rectangle,
				}
			};
			foreach(var wordplacement in workspace.Board.WordPlacements)
			{
				result = result.PlaceWord(
					wordplacement.Direction,
					wordplacement.Word,
					wordplacement.Location.X,
					wordplacement.Location.Y
				);
			}
			return result;
		}

		public static Grid GenerateGrid(this Workspace workspace)
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
			foreach (
				var slotEntry in workspace
					.SlotEntries
					.Where(
						se => 
							(se.CandidateWords?.Any() != false) // If candidate words is null, we want this to be included
					)
			)
			{
				var cell = gridCells[workspace.Board.Rectangle.IndexOf(slotEntry.Slot.Location)];
				cell.SlotEntry = slotEntry;
				cell.CellType = GridCellType.AvailableSlot;
			}
			foreach (var wordPlacement in workspace.Board.WordPlacements)
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
					gridCell.WordAndIndex= new WordAndIndex(wordPlacement.Word, i);
					if (gridCell.CellType != GridCellType.AvailableSlot)
					{
						gridCell.CellType = GridCellType.Complete;
						// Must go on either side and set it to EnforcedBlank
						// if they are already blank
						(int s1, int s2) = wordPlacement.Direction == Direction.Across
							? (moveUp(gridLocation), moveDown(gridLocation))
							: (moveLeft(gridLocation), moveRight(gridLocation));
						foreach (var s in new[] { s1, s2 })
						{
							if ((s >= 0) && (s < gridCells.Length))
							{
								var sideCell = gridCells[s];
								if (sideCell.CellType == GridCellType.Blank)
								{
									sideCell.CellType = GridCellType.BlankNoAdjacentSlots;
								}
							}
						}
					}
				}
				if (gridCells[gridLocation].CellType == GridCellType.AvailableSlot)
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
			HashSet<Slot> slotsToProcess =  GetSlotsToProcess(workspace);
			
			
			while (slotsToProcess.Any())
			{
				var slot = slotsToProcess.First();
				var gridCell = grid.CellAt(slot.Location);
				// We have to search for adjacent words
				int probe = grid.Rectangle.IndexOf(slot.Location);
				if (!(gridCells[probe].HasLetter))
				{
					throw new Exception("Invalid grid state");
				}

				PartialWord? partialWord = null;
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
						if (thisSlot != null)
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
						if (thisSlot != null)
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
			if (start < 1)
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
				var gridCell = strip.GridCells[sIndex];
				if (gridCell?.Slot?.Letter != candidateWord[cIndex])
				{
					success = false;
					continue;
				}
				var slotEntryCandidateWords = gridCell?.SlotEntry?.CandidateWords;
				if(slotEntryCandidateWords != null)
				{
					if (

						!(
							slotEntryCandidateWords
								.Contains(
									new WordAndIndex
									(
										candidateWord,
										cIndex
									)
								) == true
						)
					)
					{
						success = false;
					}
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

			foreach (var candidateWordPlacement in candidateWordPlacements)
			{
				var fullRectangle = candidateWordPlacement.GetRectangle()
					.Union(workspace.GetCurrentRectangle());
				if (RectangleIsTooBig(fullRectangle))
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

		internal static IEnumerable<Workspace> CoverSlot(this Workspace workspace, Grid grid, Slot slot) =>
			workspace.CoverFragment(
				grid,
				new string(new[] { slot.Letter }),
				slot.Location,
				slot.Direction
			);

		private static IEnumerable<Workspace> CoverSlotEntry(this Workspace workspace, Grid grid, SlotEntry slotEntry) =>
			workspace.CoverFragment(
				grid,
				new string(new[] { slotEntry.Slot.Letter }),
				slotEntry.Slot.Location,
				slotEntry.Slot.Direction
			);


		private static bool CanCoverSpace(
			this Workspace workspace,
			Grid grid,
			GridRegion space
		) =>
			GetAdjacentSlots(workspace.SlotEntries, space)
			.SelectMany(
				adj =>
					workspace.CoverSlotEntry(grid, adj)
			).Any();

		internal static bool CanCoverEachSpace(
			this Workspace workspace,
			Grid grid,
			IEnumerable<GridRegion> regions
		) =>
			regions.All(region => workspace.CanCoverSpace(grid, region));


		internal static IEnumerable<Workspace> CoverOnePartialWord(Workspace workspace, Grid grid)
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

		public static IEnumerable<WordPlacement> GetCandidateWordPlacements(
			this Workspace workspace,
			string fragment,
			Location location,
			Direction direction
			)
		{
			var availableWords = workspace.WordDatabase.ListAvailableMatchingWords(fragment);
			foreach (var candidateWord in availableWords)
			{
				yield return candidateWord.CreateWordPlacement(location, direction);
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

		internal static IEnumerable<WordPlacement> GetCandidateWordPlacements(
			this Workspace workspace,
			SlotEntry slotEntry
		) =>
			slotEntry.CandidateWords
				.Where(cw => workspace.WordDatabase.ContainsWord(cw.Word))
				.Select(cw => cw.CreateWordPlacement(slotEntry.Slot.Location, slotEntry.Slot.Direction));
			//GetCandidateWordPlacements(
			//	workspace,
			//	slotEntry.Slot.Letter.ToString(),
			//	slotEntry.Slot.Location,
			//	slotEntry.Slot.Direction
			//);

		private static bool TryCombineWorkspaces(Workspace w1, Grid w1Grid, Workspace w2, out Workspace? combined)
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
											if (
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
			Grid? grid = w1Grid;
			foreach (var w2Placement in justInW2)
			{
				grid = grid ?? w1.GenerateGrid();
				var strip = grid.GenerateStrip(w2Placement.Direction, w2Placement.Location);
				bool canPlaceWord = CanPlaceWord(strip, w2Placement.Word, strip.SlotIndex);
				if (!canPlaceWord)
				{
					return false;
				}
				Workspace? w1Attempt = w1.TryPlaceWord(grid, w2Placement);
				if (w1Attempt == null)
				{
					return false;
				}
				else
				{
					w1 = w1Attempt;
				}
				grid = null;
			}
			grid = null;
			foreach(var w1Placement in justInW1)
			{
				grid = w2.GenerateGrid();
				var strip = grid.GenerateStrip(w1Placement.Direction, w1Placement.Location);
				bool canPlaceWord = CanPlaceWord(strip, w1Placement.Word, strip.SlotIndex);
				if(!canPlaceWord)
				{
					return false;
				}
				w2 = w2.TryPlaceWord(grid, w1Placement);
				if(w2 == null)
				{
					return false;
				}
			}
			combined = w1;
			w1.SlotEntries = w1.SlotEntries.IntersectWith(w2.SlotEntries);
			var combinedGrid = combined.GenerateGrid();
			if (combinedGrid.PartialWords.Any())
			{
				combined.IsValid = false;
			}
			// TODO: Make the slot entries the intersection of the two sets of slot entries
			return true;
		}

		internal static IEnumerable<Workspace> CombineGeneratedWorkspaces(Workspace w1, IEnumerable<Workspace> g2)
		{
			var w1Grid = w1.GenerateGrid();
			foreach (var w2 in g2)
			{
				if (TryCombineWorkspaces(w1, w1Grid, w2, out Workspace? combined))
				{
					if(combined != null)
					{
						yield return combined;
					}
				}
			}
		}

		public static IEnumerable<Workspace> GetValidChildren(this Workspace workspace, INextStepGenerator nextStepGenerator)
		{
			foreach (var nextStep in nextStepGenerator.GenerateNextSteps(workspace))
			{
				if (nextStep.IsValid)
				{
					yield return nextStep;
				}
				else
				{
					foreach (var child in GetValidChildren(nextStep, nextStepGenerator))
					{
						yield return child;
					}
				}
			}
		}

		internal static IEnumerable<Workspace> CombineGeneratedWorkspaces(
			IEnumerable<Workspace> g1,
			IEnumerable<Workspace> g2,
			INextStepGenerator nextStepGenerator
		)
		{
			foreach (var w1 in g1)
			{
				foreach (var combined in CombineGeneratedWorkspaces(
					w1,
					g2
				)
				)
				{
					if(combined.IsValid)
					{
						yield return combined;
					}
					else
					{
						foreach(var validChild in combined.GetValidChildren(nextStepGenerator))
						{
							yield return validChild;
						}
					}
				}
			}
		}

		internal static IEnumerable<Workspace> CombineGeneratedWorkspaces(
			IEnumerable<IEnumerable<Workspace>> generators,
			INextStepGenerator nextStepGenerator
		)
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
							generators.Skip(1)
								.First()
								.Buffer(),
							nextStepGenerator
						)
					}.Concat(generators.Skip(2)),
					nextStepGenerator
				);
			}
		}

		private static Workspace ReplaceSlotEntry(this Workspace workspace, SlotEntry slotEntry)
		{
			var newWorkspace = workspace.Clone();
			newWorkspace.SlotEntries = newWorkspace.SlotEntries.Replace(slotEntry);
			return newWorkspace;
		}

		private static (Workspace, SlotEntry) PopulateCandidateWords(this Workspace workspace, SlotEntry slotEntry)
		{
			var candidateWords = workspace
					.WordDatabase
					.ListAvailableMatchingWords($"{slotEntry.Slot.Letter}");
			var candidateWordsHashSet = ImmutableHashSet<WordAndIndex>.Empty
				.Union(candidateWords);

			var slotEntryClone = new SlotEntry
			(
				slotEntry.Slot,
				candidateWordsHashSet
			);

			return (workspace.ReplaceSlotEntry(slotEntryClone), slotEntryClone);
		}

		private static Workspace RemoveCandidateWords(this Workspace workspace, SlotEntry slotEntry, IEnumerable<WordAndIndex> candidateWords)
		{
			var newWorkspace = workspace.Clone();
			newWorkspace.SlotEntries = newWorkspace.SlotEntries.RemoveCandidateWords(slotEntry, candidateWords);
			return newWorkspace;
		}

		private static IEnumerable<Workspace> CoverFocusedRegion(
			this Workspace workspace,
			Grid grid,
			CoverageConstraint coverageConstraint,
			GridRegion focusedRegion,
			INextStepGenerator nextStepGenerator,
			int maxPlacements
		)
		{
			var slotsToFill = GetAdjacentSlots(
				workspace.SlotEntries,
				focusedRegion // overlappingRegions.First()
			).ToList();
			while (slotsToFill.Any())
			{
				var slotEntry = slotsToFill[0];
				slotsToFill.RemoveAt(0);
				if(slotEntry.CandidateWords == null)
				{
					(workspace, slotEntry) = workspace.PopulateCandidateWords(slotEntry);
				}
				var candidateWords = slotEntry.CandidateWords;
				var wordsAndPlacements = candidateWords
					.Select(c =>
						new
						{
							CandidateWord = c,
							CandidateWordPlacement = c.CreateWordPlacement(slotEntry.Slot.Location, slotEntry.Slot.Direction)
						}
					).Where(
						wordAndPlacement =>
						{
							var rectangle = wordAndPlacement
								.CandidateWordPlacement
								.GetRectangleExcludingEndMarkers();
							return focusedRegion.OverlapsWith(rectangle);
						}
					).ToList();
				if (!wordsAndPlacements.Any())
				{
					continue;
				}
				var strip = grid.GenerateStrip(slotEntry.Slot.Direction, slotEntry.Slot.Location);
				foreach (var wordAndPlacement in wordsAndPlacements)
				{
					var candidateWordPlacement = wordAndPlacement.CandidateWordPlacement;
					if (CanPlaceWord(strip, candidateWordPlacement))
					{
						var child = workspace.TryPlaceWord(grid, candidateWordPlacement);
						if (child != null)
						{
							foreach (var grandchild in child.CoverRegion(
								coverageConstraint,
								focusedRegion,
								nextStepGenerator,
								maxPlacements-1
								)
							)
							{
								yield return grandchild;
							}
						}
					}
				}
				workspace = workspace.RemoveCandidateWords(
					slotEntry,
					wordsAndPlacements.Select(w => w.CandidateWord)
				);
			}

		}

		internal static IEnumerable<Workspace> CoverRegion(
			this Workspace workspace,
			CoverageConstraint coverageConstraint,
			GridRegion gridRegion,
			INextStepGenerator nextStepGenerator,
			int maxPlacements
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
							nextStepGenerator,
							maxPlacements-1
						)
					)
					{
						yield return grandchild;
					}
				}
				yield break;
			}
			if(maxPlacements <= 0)
			{
				yield return workspace;
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
				i => CoverFocusedRegion(
					workspace,
					grid,
					coverageConstraint,
					i,
					nextStepGenerator,
					maxPlacements
				)
			);
			foreach(
				var child in CombineGeneratedWorkspaces
				(
					generators,
					nextStepGenerator
				)
			)
			{
				yield return child;
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

		private static IEnumerable<SlotEntry> GetAdjacentSlots(
			IEnumerable<SlotEntry> slots,
			GridRegion gridRegion
		)
		{
			return slots
				.Where(s => LocationIsAdjacentOrInsideGridRegion(s.Slot.Location, gridRegion));
		}
	}
}
