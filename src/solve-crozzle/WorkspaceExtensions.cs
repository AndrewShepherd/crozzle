using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public static class WorkspaceExtensions
	{
		public static Workspace Clone(this Workspace workspace) =>
			new Workspace
			{
				Score = workspace.Score,
				Width = workspace.Width,
				Height = workspace.Height,
				XStart = workspace.XStart,
				YStart = workspace.YStart,
				Values = workspace.Values,
				AvailableWords = workspace.AvailableWords,
				WordLookup = workspace.WordLookup,
				MaxHeight = workspace.MaxHeight,
				MaxWidth = workspace.MaxWidth,
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
				slot = workspace.Slots[0];
				clone.Slots = clone.Slots.RemoveAt(0);
				return clone;
			}
		}

		public static Workspace RemoveWord(this Workspace workspace, string word)
		{
			var newWorkspace = WorkspaceExtensions.Clone(workspace);
			newWorkspace.AvailableWords = workspace.AvailableWords.Remove(word);
			return newWorkspace;
		}

		public static Location CalculateLocation(int width, int xStart, int yStart, int index) =>
			width == 0
			? new Location(0, 0)
			: new Location(
				index % width + xStart, 
				index / width + yStart
			);

		public static int CalculateIndex(int width, int xStart, int yStart, Location location) =>
			(location.y - yStart) * width + (location.x - xStart);

		public static int IndexOf(this Workspace workspace, Location location) =>
			CalculateIndex(workspace.Width, workspace.XStart, workspace.YStart, location);

		public static char CharAt(this Workspace workspace, Location location)
		{
			if ((location.x < workspace.XStart) || (location.y < workspace.YStart))
			{
				return (char)0;
			}
			int index = IndexOf(workspace, location);
			return index < workspace.Values.Length ? workspace.Values[index] : (char)0;

		}

		public static bool CanPlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			var r = workspace.GetRectangleForWord(direction, word, x, y)
				.Union(workspace.GetCurrentRectangle());
			if ((r.Width > workspace.MaxWidth) || (r.Height > workspace.MaxHeight))
				return false;

			(var startMarker, var endMarker) =
				direction == Direction.Across
				? (workspace.CharAt(new Location(x - 1, y)), workspace.CharAt(new Location(x + word.Length, y)))
				: (workspace.CharAt(new Location(x, y - 1)), workspace.CharAt(new Location(x, y + word.Length)));

			if (!((startMarker == '*') || (startMarker == (char)0)))
				return false;
			if (!((endMarker == '*') || (endMarker == (char)0)))
				return false;

			// TODO: Ask if it will make the height or width too large
			for (int i = 0; i < word.Length; ++i)
			{
				var c = direction == Direction.Down
					? workspace.CharAt(new Location(x, y + i))
					: workspace.CharAt(new Location(x + i, y));
				if (c != (char)0)
				{
					if (word[i] != c)
					{
						return false;
					}

				}
			}
			return true;
		}

		public static Workspace ExpandSize(this Workspace workspace, Rectangle newRectangle)
		{
			var currentRectangle = workspace.GetCurrentRectangle();
			var rectangle = workspace.GetCurrentRectangle().Union(newRectangle);

			var newArray = new char[rectangle.Width * rectangle.Height];
			// Now we have to calculate
			// The original point
			Location originalLocation = CalculateLocation(
				workspace.Width,
				workspace.XStart,
				workspace.YStart, 
				0
			);
			var destIndex = CalculateIndex(
				rectangle.Width,
				rectangle.MinX,
				rectangle.MinY,
				originalLocation
			);
			if(currentRectangle.Equals(rectangle))
			{
				Array.Copy(
					workspace.Values,
					newArray,
					newArray.Length
				);
			}
			else
			{
				for (
					int sourceIndex = 0;
					sourceIndex < workspace.Values.Length;
					sourceIndex += workspace.Width, destIndex += rectangle.Width
				)
				{
					Array.Copy(
						workspace.Values,
						sourceIndex,
						newArray,
						destIndex,
						workspace.Width
					);
				}
			}


			var newWorkspace = workspace.Clone();
			newWorkspace.XStart = rectangle.MinX;
			newWorkspace.YStart = rectangle.MinY;
			newWorkspace.Width = rectangle.Width;
			newWorkspace.Height = rectangle.Height;
			newWorkspace.Values = newArray;
			return newWorkspace;
		}



		public static Rectangle GetRectangleForWord(this Workspace workspace, Direction direction, string word, int x, int y) =>
			new Rectangle
			{
				MinX = x - (direction == Direction.Across ? 1 : 0),
				MaxX = direction == Direction.Across ? x + word.Length : x,
				MinY = y - (direction == Direction.Down ? 1 : 0),
				MaxY = direction == Direction.Across ? y : y + word.Length
			};

		public static Rectangle GetCurrentRectangle(this Workspace workspace) =>
			new Rectangle
			{
				MinX = workspace.XStart,
				MinY = workspace.YStart,
				MaxX = workspace.XStart + workspace.Width -1,
				MaxY = workspace.Width == 0 
					? workspace.YStart
					: workspace.YStart + (workspace.Values.Length / workspace.Width) - 1
			};

		public static Workspace PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			var rectangle = GetRectangleForWord(workspace, direction, word, x, y);
			var workspaceRectangle = workspace.GetCurrentRectangle();
			
			var newWorkspace = workspace.ExpandSize(
				rectangle
			);
			newWorkspace.AvailableWords = newWorkspace.AvailableWords.Remove(word);
			newWorkspace.IncludedWords = newWorkspace.IncludedWords.Add(word);
			newWorkspace.Score = workspace.Score + Scoring.ScorePerWord;

			int advanceIncrement = direction == Direction.Across
				? 1
				: newWorkspace.Width;

			(var startMarkerPoint, var endMarkerPoint) =
				direction == Direction.Across
				? (
					newWorkspace.IndexOf(
					new Location(x - 1, y)
					), 
					newWorkspace.IndexOf(
						new Location(x + word.Length, y)
					)
				)
				: (
					newWorkspace.IndexOf(
						new Location(x, y - 1)
					), 
					newWorkspace.IndexOf(
						new Location(x, y + word.Length)
					)
				);
			newWorkspace.Values[startMarkerPoint] = '*';
			newWorkspace.Values[endMarkerPoint] = '*';

			for (
				int i = newWorkspace.IndexOf(
					new Location(x, y)
				), 
				sIndex = 0; 
				sIndex < word.Length; 
				++sIndex,
				i+= advanceIncrement
			)
			{
				if(newWorkspace.Values[i] == word[sIndex])
				{
					newWorkspace.Score += Scoring.Score(word[sIndex]);
					newWorkspace.Intersections = newWorkspace.Intersections.Add(
						new Intersection
						{
							Word = word,
							Index = sIndex
						}
					);
				}
				else
				{
					var thisLocation = CalculateLocation(newWorkspace.Width, newWorkspace.XStart, newWorkspace.YStart, i);

					Location[] adjacencies = direction == Direction.Across
						? new[]
						{
							new Location(thisLocation.x, thisLocation.y-1),
							new Location(thisLocation.x, thisLocation.y+1)
						}
						: new[]
						{
							new Location(thisLocation.x-1, thisLocation.y),
							new Location(thisLocation.x+1, thisLocation.y)
						};
					var adjChars = adjacencies.Select(p => newWorkspace.CharAt(p));
					if(adjChars.Any(c => !(c == '*' || c == (char)0)))
					{
						newWorkspace.PartialWords = newWorkspace.PartialWords.Add(
							new PartialWord
							{
							}
						);
					}
					else
					{
						if(
							newWorkspace.WordLookup.TryGetValue(
								word.Substring(sIndex, 1), 
								out var matchingWordList
							)
						)
						{
							if(matchingWordList.Any(w => newWorkspace.AvailableWords.Contains(w)))
							{
								newWorkspace.Slots = newWorkspace.Slots.Add(
								new Slot
									{
										Direction = direction == Direction.Down ? Direction.Across : Direction.Down,
										Letter = word[sIndex],
										X = thisLocation.x,
										Y = thisLocation.y
									}
								);
							}
						}

					}

				}
				newWorkspace.Values[i] = word[sIndex];
			}
			return newWorkspace;
			
		}
		 
		public static IEnumerable<Workspace> GenerateNextSteps(this Workspace workspace)
		{
			if (workspace.PartialWords.Any())
				yield break;

			while(true)
			{
				workspace = workspace.PopSlot(out var slot);
				if(slot == null)
				{
					yield break;
				}
				if(workspace.WordLookup.TryGetValue(new string(new[] { slot.Letter }), out var flagsList))
				{
					foreach(var candidateWord in flagsList.Where(f2 => workspace.AvailableWords.Contains(f2))) 
					{
						for(int sIndex = 0; sIndex < candidateWord.Length; ++sIndex)
						{
							if(candidateWord[sIndex] == slot.Letter)
							{
								int x, y;
								if (slot.Direction == Direction.Down)
								{
									x = slot.X;
									y = slot.Y - sIndex;
								}
								else
								{
									x = slot.X - sIndex;
									y = slot.Y;
								}
								if(workspace.CanPlaceWord(slot.Direction, candidateWord, x, y))
								{
									yield return workspace.PlaceWord(slot.Direction, candidateWord, x, y);
								}
							}
						}
					}
				}
			}
		}
	}
}
