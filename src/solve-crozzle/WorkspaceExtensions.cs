﻿using System;
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
				Board = workspace.Board,
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
				partialWord = workspace.PartialWords[0];
				workspace.PartialWords = workspace.PartialWords.RemoveAt(0);
				return workspace;
			}
		}

		public static Workspace RemoveWord(this Workspace workspace, string word)
		{
			var newWorkspace = WorkspaceExtensions.Clone(workspace);
			newWorkspace.AvailableWords = workspace.AvailableWords.Remove(word);
			return newWorkspace;
		}

		public static int IndexOf(this Workspace workspace, Location location) =>
			workspace.Board.IndexOf(location);


		public static char CharAt(this Workspace workspace, Location location)
		{
			if(!(workspace.Board.Rectangle.Contains(location)))
			{
				return (char)0;
			}
			int index = IndexOf(workspace, location);
			return index < workspace.Board.Values.Length ? workspace.Board.Values[index] : (char)0;
		}

		public static bool CanPlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			var r = workspace.GetRectangleForWord(direction, word, x, y)
				.Union(workspace.GetCurrentRectangle());
			if ((r.Width > workspace.MaxWidth) || (r.Height > workspace.MaxHeight))
				return false;

			(
				var startMarkerLocation,
				var endMarkerLocation
			) = direction == Direction.Across
			? (new Location(x - 1, y), new Location(x + word.Length, y))
			: (new Location(x, y - 1), new Location(x, y + word.Length));

			var startMarker = workspace.CharAt(startMarkerLocation);
			var endMarker = workspace.CharAt(endMarkerLocation);

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
			var newWorkspace = workspace.Clone();
			newWorkspace.Board = workspace.Board.ExpandSize(newRectangle);
			return newWorkspace;
		}

		public static Rectangle GetRectangleForWord(this Workspace workspace, Direction direction, string word, int x, int y) =>
			new Rectangle(
				new Location(
					x - (direction == Direction.Across ? 1 : 0),
					y - (direction == Direction.Down ? 1 : 0)
				),
				direction == Direction.Across ? word.Length + 2 : 1,
				direction == Direction.Down ? word.Length + 2 : 1
			);

		public static Rectangle GetCurrentRectangle(this Workspace workspace) => workspace.Board.Rectangle;

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
				: newWorkspace.Board.Rectangle.Width;

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
			newWorkspace.Board.Values[startMarkerPoint] = '*';
			newWorkspace.Board.Values[endMarkerPoint] = '*';

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
				}
				else
				{
					var thisLocation = BoardExtensions.CalculateLocation(
						newWorkspace.Board.Rectangle,
						i
					);

					Location[] adjacencies = direction == Direction.Across
						? new[]
						{
							new Location(thisLocation.X, thisLocation.Y-1),
							new Location(thisLocation.X, thisLocation.Y+1)
						}
						: new[]
						{
							new Location(thisLocation.X-1, thisLocation.Y),
							new Location(thisLocation.X+1, thisLocation.Y)
						};
					var adjChars = adjacencies.Select(p => newWorkspace.CharAt(p)).ToList();
					var partialWordCharArray = new[]
					{
						adjChars[0],
						word[sIndex],
						adjChars[1]
					}.Where(c => !(c == '*' || c == (char)0))
					.ToArray();
					if (partialWordCharArray.Length > 1)
					{
						newWorkspace.PartialWords = newWorkspace.PartialWords.Add(
							new PartialWord
							{
								Direction = direction == Direction.Across ? Direction.Down : Direction.Across,
								Value = new string(partialWordCharArray),
								Location = new Location(0, 0)
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
										Location = thisLocation
									}
								);
							}
						}

					}

				}
				newWorkspace.Board.Values[i] = word[sIndex];
			}
			return newWorkspace;
			
		}

		public static IEnumerable<string> ListAvailableMatchingWords(this Workspace workspace, string word)
		{
			if (workspace.WordLookup.TryGetValue(word, out var wordList))
				return wordList.Where(w => workspace.AvailableWords.Contains(w))
					.ToList();
			else
				return Enumerable.Empty<string>();
		}

		public static IEnumerable<Workspace> CoverFragment(this Workspace workspace, string fragment, Location location, Direction direction)
		{
			var availableWords = workspace.ListAvailableMatchingWords(fragment);
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
					if (workspace.CanPlaceWord(direction, candidateWord, startLocation.X, startLocation.Y))
					{
						yield return workspace.PlaceWord(direction, candidateWord, startLocation.X, startLocation.Y);
					}
					index = candidateWord.IndexOf(fragment, index + 1);
				}

			}
		}

			public static IEnumerable<Workspace> GenerateNextSteps(this Workspace workspace)
		{
			if (workspace.PartialWords.Any())
			{
				yield break;
				workspace = workspace.PopPartialWord(out var partialSlot);
				bool successfullyPlaced = false;
				foreach(
					var solution in CoverFragment(
						workspace,
						partialSlot.Value,
						partialSlot.Location,
						partialSlot.Direction
					)
				)
				{
					successfullyPlaced = true;
					yield return solution;
				}
				if (!successfullyPlaced)
				{
					yield break;
				}
			}
			else
			{
				while (!workspace.Slots.IsEmpty)
				{
					workspace = workspace.PopSlot(out var slot);
					foreach (
						var solution in CoverFragment(
							workspace,
							new string(new[] { slot.Letter }),
							slot.Location,
							slot.Direction
						)
					)
					{
						yield return solution;
					}
				}
			}
		}
	}
}