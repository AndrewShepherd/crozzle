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

		public static bool CanPlaceWord(this Workspace workspace, Direction direction, string word, Location location)
			=> workspace.Board.CanPlaceWord(direction, word, location);

		public static Workspace ExpandSize(this Workspace workspace, Rectangle newRectangle)
		{
			var newWorkspace = workspace.Clone();
			newWorkspace.Board = workspace.Board.ExpandSize(newRectangle);
			return newWorkspace;
		}


		public static Rectangle GetCurrentRectangle(this Workspace workspace) => workspace.Board.Rectangle;

		public static Workspace PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			var wordPlacement = new WordPlacement(
				direction,
				new Location(x, y),
				word
			);
			var rectangle = wordPlacement.GetRectangle();		
			var newWorkspace = workspace.ExpandSize(
				rectangle
			);
			newWorkspace.WordDatabase = newWorkspace.WordDatabase.Remove(word);
			newWorkspace.IncludedWords = newWorkspace.IncludedWords.Add(word);
			newWorkspace.Score = workspace.Score + Scoring.ScorePerWord;
			int advanceIncrement = direction == Direction.Across
				? 1
				: newWorkspace.Board.Rectangle.Width;

			(var startMarkerPoint, var endMarkerPoint) =
				direction == Direction.Across
				? (
					newWorkspace.Board.IndexOf(
					new Location(x - 1, y)
					), 
					newWorkspace.Board.IndexOf(
						new Location(x + word.Length, y)
					)
				)
				: (
					newWorkspace.Board.IndexOf(
						new Location(x, y - 1)
					), 
					newWorkspace.Board.IndexOf(
						new Location(x, y + word.Length)
					)
				);
			newWorkspace.Board.Values[startMarkerPoint] = '*';
			newWorkspace.Board.Values[endMarkerPoint] = '*';

			for (
				int i = newWorkspace.Board.IndexOf(
					new Location(x, y)
				), 
				sIndex = 0; 
				sIndex < word.Length; 
				++sIndex,
				i+= advanceIncrement
			)
			{
				if(newWorkspace.Board.Values[i] == word[sIndex])
				{
					newWorkspace.Score += Scoring.Score(word[sIndex]);
					newWorkspace.Intersections = newWorkspace.Intersections.Add(
						new Intersection
						{
							Word = word,
							Index = sIndex
						}
					);
					var location = newWorkspace.Board.Rectangle.CalculateLocation(i);
					var matchingSlot = newWorkspace
						.Slots
						.FirstOrDefault(
							s => s.Location.Equals(location)
						);
					if(matchingSlot != null)
					{
						newWorkspace.Slots = newWorkspace.Slots.Remove(matchingSlot);
					}
				}
				else
				{
					newWorkspace.Board.Values[i] = word[sIndex];

					var thisLocation = newWorkspace.Board.Rectangle.CalculateLocation(i);
					
					PartialWord partialWord = newWorkspace.Board.GetContiguousTextAt(
						direction == Direction.Across ? Direction.Down : Direction.Across,
						thisLocation
					);
					if (partialWord.Value.Length > 1)
					{
						foreach(var pw2 in newWorkspace.PartialWords)
						{
							if(pw2.Direction == partialWord.Direction)
							{
								if(pw2.Rectangle.IntersectsWith(partialWord.Rectangle))
								{
									newWorkspace.PartialWords = newWorkspace.PartialWords.Remove(pw2);
								}
							}
						}
						newWorkspace.PartialWords = newWorkspace.PartialWords.Add(
							partialWord
						);
						newWorkspace.Slots = newWorkspace.Slots.Add(
							new Slot
								(
									word[sIndex],
									direction == Direction.Down ? Direction.Across : Direction.Down,
									thisLocation
								)
							);
					}
					else
					{
						if(newWorkspace.WordDatabase.CanMatchWord(word.Substring(sIndex, 1)))
						{
							newWorkspace.Slots = newWorkspace.Slots.Add(
								new Slot
									(
										word[sIndex],
										direction == Direction.Down ? Direction.Across : Direction.Down,
										thisLocation
									)
							);
						}
					}

				}
			}
			newWorkspace.Board = newWorkspace.Board.PlaceWord(
				direction,
				new Location(x, y),
				word
			);
			return newWorkspace;			
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
					if (workspace.CanPlaceWord(direction, candidateWord, startLocation))
					{
						var newWorkspace = workspace.PlaceWord(direction, candidateWord, startLocation.X, startLocation.Y);
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

		public class Strip
		{
			public char[] Characters;
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
					if(!(gridCell.CellType == GridCellType.AvailableSlot))
					{
						gridCell.CellType = GridCellType.Complete;
					}
				}
				gridCells[gridLocation].CellType = GridCellType.EnforcedBlank;
			}
			return new Grid
			{
				Rectangle = workspace.Board.Rectangle,
				Cells = gridCells
			};
		}

		public static Strip GenerateStrip(this Workspace workspace, Slot slot)
		{
			var rectangle = workspace.Board.Rectangle;
			long amountToBeAdded = slot.Direction == Direction.Across
				? Board.MaxWidth - rectangle.Width
				: Board.MaxHeight - rectangle.Height;
			if (slot.Direction == Direction.Across)
			{
				int indexFirst = workspace.Board.Rectangle.Right - Board.MaxWidth + 1;
				int indexLast = workspace.Board.Rectangle.Left + Board.MaxWidth - 1;
				var length = indexLast - indexFirst + 1;
				var characters = new char[length];
				var slotIndex = slot.Location.X - indexFirst;
				characters[0] = '*';
				characters[characters.Length - 1] = '*';
				characters[slotIndex] = slot.Letter;

				for (int i = 1; i < (characters.Length - 1); ++i)
				{
					if (i == slotIndex)
						continue;
					var l = new Location(i + indexFirst, slot.Location.Y);
					var c = workspace.Board.CharAt(l);
					switch (c)
					{
						case '*':
							characters[i] = c;
							break;
						case (char)0:
							characters[i] = c;
							break;
						default:
							if (
								workspace
									.Slots
									.Where(s => s.Direction == slot.Direction)
									.Where(s => s.Location.Equals(l))
									.Any()
							)
							{
								characters[i] = c;
							}
							else
							{
								characters[i] = '$';
							}
							break;
					}
				}
				return new Strip
				{
					Characters = characters,
					StartAt = indexFirst,
					SlotIndex = slotIndex
				};
			}
			else
			{
				int indexFirst = workspace.Board.Rectangle.Bottom - Board.MaxHeight + 1;
				int indexLast = workspace.Board.Rectangle.Top + Board.MaxHeight - 1;
				var length = indexLast - indexFirst + 1;
				var characters = new char[length];
				var slotIndex = slot.Location.Y - indexFirst;
				characters[0] = '*';
				characters[characters.Length-1] = '*';
				characters[slotIndex] = slot.Letter;
				for(int i = 1; i < (characters.Length-1); ++i)
				{
					if (i == slotIndex)
						continue;
					var l = new Location(slot.Location.X, indexFirst + i);
					var c = workspace.Board.CharAt(l);
					switch(c)
					{
						case '*':
							characters[i] = c;
							break;
						case (char)0:
							characters[i] = c;
							break;
						default:
							if (
								workspace
									.Slots
									.Where(s => s.Direction == slot.Direction)
									.Where(s => s.Location.Equals(l))
									.Any()
							)
							{
								characters[i] = c;
							}
							else
							{
								characters[i] = '$';
								++i;
							}
							break;
					}
				}
				var strip = new Strip
				{
					Characters = characters,
					StartAt = indexFirst,
					SlotIndex = slotIndex
				};


				// Find the last dollar sign before the slotIndex
				int indexOfFirstDollarBeforeSlotIndex = slotIndex - 1;
				for (;
					indexOfFirstDollarBeforeSlotIndex >= 0
					&&
					characters[indexOfFirstDollarBeforeSlotIndex] != '$';
					--indexOfFirstDollarBeforeSlotIndex
				);
				if(indexOfFirstDollarBeforeSlotIndex != -1)
				{
					strip = new Strip
					{
						Characters = strip.Characters.Skip(indexOfFirstDollarBeforeSlotIndex + 1).ToArray(),
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
					var strip = workspace.GenerateStrip(slot);

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
							var charBefore = strip.Characters[start - 1];
							if (!((charBefore == 0) || (charBefore == '*')))
								continue;
							if (start + candidateWord.Length > (strip.Characters.Length-1))
								continue;
							var charAfter = strip.Characters[start + candidateWord.Length];
							if (!((charAfter == 0) || (charAfter == '*')))
								continue;
							bool success = true;
							for(
								int cIndex = 0, sIndex=start;
								cIndex < candidateWord.Length && success;
								++cIndex, ++sIndex
							)
							{
								if(strip.Characters[sIndex] == 0)
								{
									continue;
								}
								if(strip.Characters[sIndex] != candidateWord[cIndex])
								{
									success = false;
								}
							}
							if(success)
							{

								Location l = slot.Direction == Direction.Across
									? new Location(strip.StartAt + start, slot.Location.Y)
									: new Location(slot.Location.X, strip.StartAt + start);
								//bool canPlaceWord = workspace.CanPlaceWord(
								//	slot.Direction,
								//	candidateWord,
								//	l
								//);
								//if(!canPlaceWord)
								//{
								//	int dummy = 3;
								//}

								
								var child = workspace.PlaceWord(
									slot.Direction,
									candidateWord, 
									l.X, 
									l.Y
								);
								yield return child.Normalise();

							}
						}
					}
				}
			}
		}
	}
}
