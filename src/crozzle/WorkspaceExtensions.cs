namespace crozzle
{
	using System;
	using System.Collections.Generic;
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
				PartialWords = workspace.PartialWords,
				IncludedWords = workspace.IncludedWords,
				Intersections = workspace.Intersections
			};

		public static Workspace PopSlot(this Workspace workspace, out Slot slot)
		{
			if(workspace.Slots.IsEmpty)
			{
				slot = null;
				return workspace;
			}
			else
			{
				var clone = workspace.Clone();
				slot = workspace.Slots.OrderByDescending(s => Scoring.Score(s.Letter)).First();
				clone.Slots = clone.Slots.Remove(slot);
				return clone;
			}
		}

		public static Workspace PopPartialWord(this Workspace workspace, out PartialWord partialWord)
		{
			if(workspace.PartialWords.IsEmpty)
			{
				partialWord = null;
				return workspace;
			}
			else
			{
				var clone = workspace.Clone();
				partialWord = workspace.PartialWords.OrderByDescending(pw => pw.Value.Length).First();
				clone.PartialWords = workspace.PartialWords.Remove(partialWord);
				return clone;
			}
		}

		public static Workspace RemoveWord(this Workspace workspace, string word)
		{
			var newWorkspace = WorkspaceExtensions.Clone(workspace);
			newWorkspace.WordDatabase = workspace.WordDatabase.Remove(word); 
			return newWorkspace;
		}

		public static int IndexOf(this Workspace workspace, Location location) =>
			workspace.Board.IndexOf(location);

		public static char CharAt(this Workspace workspace, Location location) =>
			workspace.Board.CharAt(location);

		public static Workspace ExpandSize(this Workspace workspace, Rectangle newRectangle)
		{
			var newWorkspace = workspace.Clone();
			newWorkspace.Board = workspace.Board.ExpandSize(newRectangle);
			return newWorkspace;
		}


		public static Rectangle GetCurrentRectangle(this Workspace workspace) => workspace.Board.Rectangle;

		public static Workspace PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y) =>
			PlaceWord(
				workspace,
				workspace.GenerateGrid(),
				new WordPlacement(
					direction,
					new Location(x, y),
					word
				)
			);

		// Places the word in the workspace
		// 
		// The new workspace will have
		//  - Different Slots
		//  - Different Partial Words
		//  - Different wordplacements
		//  - Different available words
		//  - Different Score
		internal static Workspace PlaceWord(this Workspace workspace, Grid grid, WordPlacement wordPlacement)
		{
			var rectangle = wordPlacement.GetRectangle();		
			var newWorkspace = workspace.ExpandSize(
				rectangle
			);
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
					// Must now check for partial words
					PartialWord partialWord;
					if(wordPlacement.Direction == Direction.Across)
					{
						partialWord = new PartialWord
						{
							Direction = Direction.Down,
							Rectangle = new Rectangle(l, l),
							Value = wordPlacement.Word[stringIndex].ToString()
						};
						if(gridCell.PartialWordAbove != null)
						{
							partialWord = new PartialWord
							{
								Direction = Direction.Down,
								Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordAbove.Rectangle),
								Value = $"{gridCell.PartialWordAbove.Value}{partialWord.Value}",
							};
						}
						if(gridCell.PartialWordBelow != null)
						{
							partialWord = new PartialWord
							{
								Direction = Direction.Down,
								Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordBelow.Rectangle),
								Value = $"{partialWord.Value}{gridCell.PartialWordBelow.Value}",
							};
						}
					}
					else
					{
						partialWord = new PartialWord
						{
							Direction = Direction.Across,
							Rectangle = new Rectangle(l, l),
							Value = wordPlacement.Word[stringIndex].ToString()
						};
						if (gridCell.PartialWordToLeft != null)
						{
							partialWord = new PartialWord
							{
								Direction = Direction.Across,
								Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordToLeft.Rectangle),
								Value = $"{gridCell.PartialWordToLeft.Value}{partialWord.Value}",
							};
						}
						if (gridCell.PartialWordToRight != null)
						{
							partialWord = new PartialWord
							{
								Direction = Direction.Across,
								Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordToRight.Rectangle),
								Value = $"{partialWord.Value}{gridCell.PartialWordToRight.Value}",
							};
						}
					}
					if(partialWord.Value.Length > 1)
					{
						newWorkspace.PartialWords = newWorkspace.PartialWords.Add(partialWord);
					}
				}
				else
				{
					throw new Exception("Unexpected state");
				}
			}
			newWorkspace.Board = newWorkspace.Board.PlaceWord(
				wordPlacement
			);
			return newWorkspace;			
		}

		private static PartialWord MergePartialWords(PartialWord partialWordAbove, char v, PartialWord partialWordBelow)
		{
			if((partialWordAbove == null) && (partialWordBelow == null))
			{
				return null;
			}
			string s = $"{partialWordAbove?.Value ?? string.Empty}{v}{partialWordBelow?.Value ?? string.Empty}";
			var direction = partialWordAbove != null ? partialWordAbove.Direction : partialWordBelow.Direction;
			Rectangle rectangle = partialWordAbove?.Rectangle;
			if(rectangle != null)
			{
				if(direction == Direction.Across)
				{
					rectangle = new Rectangle(rectangle.TopLeft, new Location(rectangle.Right + 1, rectangle.Top));
				}
				else
				{
					rectangle = new Rectangle(rectangle.TopLeft, new Location(rectangle.Left, rectangle.Bottom + 1));
				}
				if(partialWordBelow != null)
				{
					rectangle = rectangle.Union(partialWordBelow.Rectangle);
				}
			}
			else
			{
				rectangle = partialWordBelow.Rectangle;
				if(direction == Direction.Across)
				{
					rectangle = new Rectangle(
						new Location(rectangle.Left - 1, rectangle.Top),
						new Location(rectangle.Right, rectangle.Top)
					);
				}
				else
				{
					rectangle = new Rectangle(
						new Location(rectangle.Left, rectangle.Top - 1),
						new Location(rectangle.Right, rectangle.Top)
					);
				}
			}
			return new PartialWord
			{
				Direction = direction == Direction.Across ? Direction.Down : Direction.Across,
				Rectangle = rectangle,
				Value = s
			};
		}

		internal static IEnumerable<Workspace> CoverFragment(
			this Workspace workspace,
			Grid grid,
			string fragment,
			Location location,
			Direction direction
		)
		{
			var availableWords = workspace.WordDatabase.ListAvailableMatchingWords(fragment);
			foreach (var candidateWord in availableWords)
			{
				var index = candidateWord.IndexOf(fragment, 0);
				while (index != -1)
				{
					Location startLocation;
					if (direction == Direction.Down)
					{
						startLocation = new Location(location.X, location.Y - index);
					}
					else
					{
						startLocation = new Location(location.X - index, location.Y);
					}
					
					if (grid.CanPlaceWord(direction, candidateWord, startLocation))
					{
						var newWorkspace = workspace.PlaceWord(
							grid,
							new WordPlacement(
								direction,
								startLocation,
								candidateWord
							)
						);
						// Quick check of the partial words
						if(newWorkspace
							.PartialWords
							.All(
								pw => newWorkspace.WordDatabase.CanMatchWord(pw.Value)
							)
						)
						{
							yield return newWorkspace;
						}
						else
						{
							int dummy = 3;
						}
					}
					index = candidateWord.IndexOf(fragment, index + 1);
				}

			}
		}

		internal class Strip
		{
			public GridCell[] GridCells;
			public int StartAt;

			public int SlotIndex { get; internal set; }
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
				gridCells[back(gridLocation)].CellType = GridCellType.EnforcedBlank;
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
									sideCell.CellType = GridCellType.EnforcedBlank;
								}
							}
						}
					}
				}
				gridCells[gridLocation].CellType = GridCellType.EnforcedBlank;
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
				PartialWord partialWord = null;
				if (gridCell.Slot.Direction == Direction.Across)
				{
					while (gridCells[moveRight(probe)].CellType == GridCellType.AvailableSlot)
					{
						probe = moveRight(probe);
					}
					int rightNonSlotLocation = moveRight(probe);
					Location rectangleBottomRight = workspace.Board.Rectangle.CalculateLocation(probe);
					string contiguousText = string.Empty;
					Location rectangleTopLeft = rectangleBottomRight;
					while (gridCells[probe].CellType == GridCellType.AvailableSlot)
					{
						var thisSlot = gridCells[probe].Slot;
						if(thisSlot.Direction != Direction.Across)
						{
							throw new Exception("Unexpected state reached");
						}
						slotsToProcess.Remove(thisSlot);
						contiguousText = $"{thisSlot.Letter}{contiguousText}";
						probe = moveLeft(probe);
						rectangleTopLeft = workspace.Board.Rectangle.CalculateLocation(probe);
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
					while (gridCells.CellAt(moveDown(probe)).CellType == GridCellType.AvailableSlot)
					{
						probe = moveDown(probe);
					}
					int bottomNonSlotLocation = moveDown(probe);
					Location rectangleBottomRight = workspace.Board.Rectangle.CalculateLocation(probe);
					string contiguousText = string.Empty;
					Location rectangleTopLeft = rectangleBottomRight;
					while (gridCells.CellAt(probe).CellType == GridCellType.AvailableSlot)
					{
						var thisSlot = gridCells[probe].Slot;
						if (thisSlot.Direction != Direction.Down)
						{
							throw new Exception("Unexpected state reached");
						}
						slotsToProcess.Remove(thisSlot);
						contiguousText = $"{thisSlot.Letter}{contiguousText}";
						probe = moveUp(probe);
						rectangleTopLeft = workspace.Board.Rectangle.CalculateLocation(probe);
					}
					partialWord = new PartialWord
					{
						Direction = Direction.Across,
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

		internal static Strip GenerateStrip(this Workspace workspace, Grid grid, Slot slot)
		{
			var rectangle = grid.Rectangle;
			long amountToBeAdded = slot.Direction == Direction.Across
				? Board.MaxWidth - rectangle.Width
				: Board.MaxHeight - rectangle.Height;
			if (slot.Direction == Direction.Across)
			{
				int indexFirst = grid.Rectangle.Right - Board.MaxWidth + 1;
				int indexLast = grid.Rectangle.Left + Board.MaxWidth - 1;
				var length = indexLast - indexFirst + 1;
				var gridCells = new GridCell[length];
				var slotIndex = slot.Location.X - indexFirst;
				gridCells[0] = GridCell.EnforcedBlank;
				gridCells[gridCells.Length - 1] = GridCell.EnforcedBlank;
				gridCells[slotIndex] = GridCell.FromSlot(slot);

				for (int i = 1; i < (gridCells.Length - 1); ++i)
				{
					if (i == slotIndex)
						continue;
					var l = new Location(i + indexFirst, slot.Location.Y);
					var gridCell = grid.CellAt(l);
					gridCells[i] = gridCell;
				}
				return new Strip
				{
					GridCells = gridCells,
					StartAt = indexFirst,
					SlotIndex = slotIndex
				};
			}
			else
			{
				int indexFirst = grid.Rectangle.Bottom - Board.MaxHeight + 1;
				int indexLast = grid.Rectangle.Top + Board.MaxHeight - 1;
				var length = indexLast - indexFirst + 1;
				var gridCells = new GridCell[length];
				var slotIndex = slot.Location.Y - indexFirst;
				gridCells[0] = GridCell.EnforcedBlank;
				gridCells[gridCells.Length - 1] = GridCell.EnforcedBlank;
				gridCells[slotIndex] = GridCell.FromSlot(slot);
				for (int i = 1; i < (gridCells.Length - 1); ++i)
				{
					if (i == slotIndex)
						continue;
					var l = new Location(slot.Location.X, indexFirst + i);
					var gridCell = grid.CellAt(l);
					gridCells[i] = gridCell;
				}
				var strip = new Strip
				{
					GridCells = gridCells,
					StartAt = indexFirst,
					SlotIndex = slotIndex
				};


				// Find the last dollar sign before the slotIndex
				int indexOfFirstDollarBeforeSlotIndex = slotIndex - 1;
				for (;
					indexOfFirstDollarBeforeSlotIndex >= 0
					&&
					gridCells[indexOfFirstDollarBeforeSlotIndex].CellType != GridCellType.Complete;
					--indexOfFirstDollarBeforeSlotIndex
				);
				if(indexOfFirstDollarBeforeSlotIndex != -1)
				{
					strip = new Strip
					{
						GridCells = strip.GridCells.Skip(indexOfFirstDollarBeforeSlotIndex + 1).ToArray(),
						SlotIndex = strip.SlotIndex - (indexOfFirstDollarBeforeSlotIndex + 1),
						StartAt = strip.StartAt + indexOfFirstDollarBeforeSlotIndex + 1
					};
				}
				return strip;
			}
		}

		public static IEnumerable<Workspace> GenerateNextSteps(this Workspace workspace)
		{
			var grid = workspace.GenerateGrid();
			if (workspace.PartialWords.Any())
			{
				workspace = workspace.PopPartialWord(out var partialSlot);
				foreach(
					var solution in CoverFragment(
						workspace,
						grid,
						partialSlot.Value,
						partialSlot.Rectangle.TopLeft,
						partialSlot.Direction
					)
				)
				{
					yield return solution.Normalise();
				}
				yield break;
			}
			else
			{
				while (!workspace.Slots.IsEmpty)
				{
					workspace = workspace.PopSlot(out var slot);
					var availableWords = workspace.WordDatabase.ListAvailableMatchingWords($"{slot.Letter}");
					if (!(availableWords.Any()))
					{
						continue;
					}
					var strip = workspace.GenerateStrip(grid, slot);

					foreach (var candidateWord in availableWords)
					{
						var maxLength = (
							slot.Direction == Direction.Across
							? Board.MaxWidth 
							: Board.MaxHeight
						) - 2;
						if (candidateWord.Length > maxLength)
							continue;
						for (
							int index = candidateWord.IndexOf(slot.Letter, 0);
							index != -1;
							index = candidateWord.IndexOf(slot.Letter, index + 1)
						)
						{
							var start = strip.SlotIndex - index;
							if (start < 1)
								continue;
							var charBefore = strip.GridCells[start - 1];
							if (!((charBefore.CellType == GridCellType.Blank) || (charBefore.CellType == GridCellType.EnforcedBlank)))
								continue;
							if (start + candidateWord.Length > (strip.GridCells.Length-1))
								continue;
							var charAfter = strip.GridCells[start + candidateWord.Length];
							if (!((charAfter.CellType == GridCellType.Blank) || (charAfter.CellType == GridCellType.EnforcedBlank)))
								continue;
							bool success = true;
							for(
								int cIndex = 0, sIndex=start;
								cIndex < candidateWord.Length && success;
								++cIndex, ++sIndex
							)
							{
								if(strip.GridCells[sIndex].CellType == GridCellType.Blank)
								{
									continue;
								}
								if(strip.GridCells[sIndex].Slot?.Letter != candidateWord[cIndex])
								{
									success = false;
								}
							}
							if(success)
							{

								Location l = slot.Direction == Direction.Across
									? new Location(strip.StartAt + start, slot.Location.Y)
									: new Location(slot.Location.X, strip.StartAt + start);
								var child = workspace.PlaceWord(
									grid,
									new WordPlacement(
										slot.Direction,
										l,
										candidateWord
									)
								);
								yield return child.Normalise();
							}
						}
					}


					grid.RemoveSlot(slot);
				}
			}
		}
	}
}
