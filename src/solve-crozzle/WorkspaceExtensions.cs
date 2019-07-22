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
				WordToFlagMap = workspace.WordToFlagMap,
				FlagToWordMap = workspace.FlagToWordMap,
				Slots = workspace.Slots,
				PartialWords = workspace.PartialWords
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
			newWorkspace.AvailableWords = workspace.AvailableWords & ~workspace.WordToFlagMap[word];
			return newWorkspace;
		}

		public static (int x, int y) CalculateLocation(int width, int xStart, int yStart, int index) =>
			width == 0
			? (0, 0)
			: (
				index % width + xStart, 
				index / width + yStart
			);

		public static int CalculateIndex(int width, int xStart, int yStart, int x, int y) =>
			(y - yStart) * width + (x - xStart);

		public static int IndexOf(this Workspace workspace, int x, int y) =>
			CalculateIndex(workspace.Width, workspace.XStart, workspace.YStart, x, y);

		public static char CharAt(this Workspace workspace, int x, int y)
		{
			if ((x < workspace.XStart) || (y < workspace.YStart))
			{
				return (char)0;
			}
			int index = IndexOf(workspace, x, y);
			return index < workspace.Values.Length ? workspace.Values[index] : (char)0;

		}

		public static bool CanPlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			(var startMarker, var endMarker) =
				direction == Direction.Across
				? (workspace.CharAt(x - 1, y), workspace.CharAt(x + word.Length, y))
				: (workspace.CharAt(x, y - 1), workspace.CharAt(x, y + word.Length));

			if (!((startMarker == '*') || (startMarker == (char)0)))
				return false;
			if (!((endMarker == '*') || (endMarker == (char)0)))
				return false;

			// TODO: Ask if it will make the height or width too large
			for (int i = 0; i < word.Length; ++i)
			{
				var c = direction == Direction.Down
					? workspace.CharAt(x, y + i)
					: workspace.CharAt(x + i, y);
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

		public static Workspace ExpandSize(this Workspace workspace, int minX, int minY, int maxX, int maxY)
		{
			minX = Math.Min(workspace.XStart, minX);
			minY = Math.Min(workspace.YStart, minY);
			maxX = Math.Max(workspace.Width, maxX);
			maxY = workspace.Width == 0
				? maxY 
				: Math.Max(workspace.Values.Length / workspace.Width + workspace.YStart, maxY);
			var newWidth = maxX - minX + 1;
			var newHeight = maxY - minY + 1;

			var newArray = new char[newWidth * newHeight];
			// Now we have to calculate
			// The original point
			(int originalX, int originalY) = CalculateLocation(workspace.Width, workspace.XStart, workspace.YStart, 0);
			var destIndex = CalculateIndex(newWidth, minX, minY, originalX, originalY);
			for(
				int sourceIndex = 0;
				sourceIndex < workspace.Values.Length;
				sourceIndex += workspace.Width, destIndex += newWidth
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

			var newWorkspace = workspace.Clone();
			newWorkspace.XStart = minX;
			newWorkspace.YStart = minY;
			newWorkspace.Width = newWidth;
			newWorkspace.Height = maxY - minY + 1;
			newWorkspace.Values = newArray;
			return newWorkspace;
		}

		public static Workspace PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			// Figure out the new dimensions
			// create a new array
			var maxX = direction == Direction.Across ? x + word.Length : x;
			var maxY = direction == Direction.Across ? y : y + word.Length;

			var newWorkspace = workspace.ExpandSize(
				x - (direction == Direction.Across ? 1 : 0), 
				y - (direction == Direction.Down ? 1 : 0), 
				maxX, 
				maxY
			);
			int advanceIncrement = direction == Direction.Across
				? 1
				: newWorkspace.Width;

			(var startMarkerPoint, var endMarkerPoint) =
				direction == Direction.Across
				? (newWorkspace.IndexOf(x - 1, y), newWorkspace.IndexOf(x + word.Length, y))
				: (newWorkspace.IndexOf(x, y - 1), newWorkspace.IndexOf(x, y + word.Length));
			newWorkspace.Values[startMarkerPoint] = '*';
			newWorkspace.Values[endMarkerPoint] = '*';

			for (int i = newWorkspace.IndexOf(x, y), sIndex = 0; sIndex < word.Length; ++sIndex, i+= advanceIncrement)
			{
				if(newWorkspace.Values[i] == word[sIndex])
				{
					newWorkspace.Score += Scoring.Score(word[sIndex]);
				}
				else
				{
					var thisLocation = CalculateLocation(newWorkspace.Width, newWorkspace.XStart, newWorkspace.YStart, i);
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
				newWorkspace.Score += newWorkspace.Values[i] == word[sIndex]
					? Scoring.Score(word[sIndex])
					: 0;
				newWorkspace.Values[i] = word[sIndex];
			}
			newWorkspace.Score += 1;
			newWorkspace.AvailableWords = newWorkspace.AvailableWords & ~(newWorkspace.WordToFlagMap[word]);
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
					foreach(var f in flagsList.Where(f2 => (f2&workspace.AvailableWords) != 0)) 
					{
						var candidateWord = workspace.FlagToWordMap[f];
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
