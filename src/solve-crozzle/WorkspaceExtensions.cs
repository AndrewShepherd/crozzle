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
				Board = workspace.Board,
				AvailableWords = workspace.AvailableWords,
				WordLookup = workspace.WordLookup,
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
				clone.PartialWords = workspace.PartialWords.RemoveAt(0);
				return clone;
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

		public static char CharAt(this Workspace workspace, Location location) =>
			workspace.Board.CharAt(location);

		public static bool CanPlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
			=> workspace.Board.CanPlaceWord(direction, word, new Location(x, y));

		public static Workspace ExpandSize(this Workspace workspace, Rectangle newRectangle)
		{
			var newWorkspace = workspace.Clone();
			newWorkspace.Board = workspace.Board.ExpandSize(newRectangle);
			return newWorkspace;
		}


		public static Rectangle GetCurrentRectangle(this Workspace workspace) => workspace.Board.Rectangle;

		public static Workspace PlaceWord(this Workspace workspace, Direction direction, string word, int x, int y)
		{
			var rectangle = BoardExtensions.GetRectangleForWord(direction, word, x, y);			
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
				}
				else
				{
					newWorkspace.Board.Values[i] = word[sIndex];

					var thisLocation = newWorkspace.Board.Rectangle.CalculateLocation(i);
					
					PartialWord partialWord = newWorkspace.Board.GetWordAt(
						direction == Direction.Across ? Direction.Down : Direction.Across,
						thisLocation
					);
					if (partialWord.Value.Length > 1)
					{
						newWorkspace.PartialWords = newWorkspace.PartialWords.Add(
							partialWord
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
				workspace = workspace.PopPartialWord(out var partialSlot);
				foreach(
					var solution in CoverFragment(
						workspace,
						partialSlot.Value,
						partialSlot.Rectangle.TopLeft,
						partialSlot.Direction
					)
				)
				{
					yield return solution;
				}
				yield break;
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
