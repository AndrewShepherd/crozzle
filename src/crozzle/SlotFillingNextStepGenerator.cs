﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace crozzle
{
	public class SlotFillingNextStepGenerator : INextStepGenerator
	{
		public class IndexAndDirection
		{
			public int Index { get; set; }
			public Direction Direction { get; set; }

			public override bool Equals(object? obj) =>
				object.ReferenceEquals(this, obj)
				|| (
					(obj is IndexAndDirection other)
					&& other.Index == this.Index
					&& other.Direction == this.Direction
				);

			public override int GetHashCode() =>
				Index << 8 ^ Direction.GetHashCode();
		}

		private readonly int _minAdjacentGroupSize;

		public SlotFillingNextStepGenerator(int minAdjacentGroupSize)
		{
			_minAdjacentGroupSize = minAdjacentGroupSize;
		}

		Dictionary<IndexAndDirection, List<SlotEntry>> CategorizeSlotEntries(IEnumerable<SlotEntry> slotEntries)
		{
			var categorizedSlots = new System.Collections.Generic.Dictionary<IndexAndDirection, List<SlotEntry>>();
			foreach (var slotEntry in slotEntries)
			{
				var indexAndDirection = new IndexAndDirection
				{
					Direction = slotEntry.Slot.Direction,
					Index = slotEntry.Slot.Direction == Direction.Across
					? slotEntry.Slot.Location.X
					: slotEntry.Slot.Location.Y
				};
				if (categorizedSlots.TryGetValue(indexAndDirection, out var entries))
				{
					entries.Add(slotEntry);
				}
				else
				{
					categorizedSlots.Add(
						indexAndDirection,
						new List<SlotEntry>() { slotEntry }
					);
				}
			}
			return categorizedSlots;
		}

		static IEnumerable<IEnumerable<T>> GetAdjacentGroups<T>(IEnumerable<T> items, Func<T, int> getIndex)
		{
			if(!items.Any())
			{
				yield break;
			}
			var sorted = items.OrderBy(item => getIndex(item));
			var list = new List<T>() { sorted.First() };
			foreach(var item in sorted.Skip(1))
			{
				var previousItem = list.Last();
				var previousIndex = getIndex(previousItem);
				var thisIndex = getIndex(item);
				if(thisIndex == previousIndex+1)
				{
					list.Add(item);
				}
				else
				{
					yield return list;
					list = new List<T>() { item };
				}
			}
			yield return list;
		}

		IEnumerable<IEnumerable<SlotEntry>> GetAdjacentGroups(IEnumerable<SlotEntry> slotEntries)
		{
			var categorized = CategorizeSlotEntries(slotEntries);
			foreach(var kvp in categorized)
			{
				Func<SlotEntry, int> getIndex = kvp.Key.Direction == Direction.Down
					? new Func<SlotEntry, int>(se => se.Slot.Location.X)
					: se => se.Slot.Location.Y;
				foreach(var group in GetAdjacentGroups(kvp.Value, getIndex))
				{
					yield return group;
				}
			}
		}

		private class CompareBasedOnProximityToCorner : IComparer<IEnumerable<SlotEntry>>
		{
			private static int CalculateProximity(Location l) =>
				l.X * l.X + l.Y * l.Y;

			private static int CalculateProximity(IEnumerable<SlotEntry> sle) =>
				CalculateProximity(sle.First().Slot.Location);
			int IComparer<IEnumerable<SlotEntry>>.Compare(IEnumerable<SlotEntry>? x, IEnumerable<SlotEntry>? y) =>
				CalculateProximity(x).CompareTo(CalculateProximity(y));
		}

		private class FirstAddedSlotEntry : IComparer<IEnumerable<SlotEntry>>
		{
			readonly IEnumerable<SlotEntry> _slotEntriesInOrder;

			public FirstAddedSlotEntry(IEnumerable<SlotEntry> orderedSlotEntries)
			{
				_slotEntriesInOrder = orderedSlotEntries;
			}
			int IComparer<IEnumerable<SlotEntry>>.Compare(IEnumerable<SlotEntry>? x, IEnumerable<SlotEntry>? y)
			{
				foreach(var slotEntry in _slotEntriesInOrder)
				{
					if(slotEntry.Slot.Equals(x.First().Slot))
					{
						return -1;
					}
					if(slotEntry.Slot.Equals(y.First().Slot))
					{
						return 1;
					}
				}
				return 0;
			}
		}

		private class LastAddedSlotEntry : IComparer<IEnumerable<SlotEntry>>
		{
			readonly IEnumerable<SlotEntry> _slotEntriesInOrder;

			public LastAddedSlotEntry(IEnumerable<SlotEntry> orderedSlotEntries)
			{
				_slotEntriesInOrder = orderedSlotEntries;
			}
			int IComparer<IEnumerable<SlotEntry>>.Compare(IEnumerable<SlotEntry>? x, IEnumerable<SlotEntry>? y)
			{
				foreach (var slotEntry in _slotEntriesInOrder)
				{
					if (slotEntry.Slot.Equals(x.First().Slot))
					{
						return 1;
					}
					if (slotEntry.Slot.Equals(y.First().Slot))
					{
						return -1;
					}
				}
				return 0;
			}
		}

		private WordPlacement GetWordPlacement(Workspace workspace, SlotEntry slotEntry)
		{
			foreach(var wordPlacement in workspace.Board.WordPlacements)
			{
				if(slotEntry.Slot.Direction == wordPlacement.Direction)
				{
					continue;
				}
				if(wordPlacement.GetRectangle().Contains(slotEntry.Slot.Location))
				{
					return wordPlacement;
				}
			}
			return null;
		}

		IEnumerable<Workspace> INextStepGenerator.GenerateNextSteps(Workspace workspace)
		{
			var grid = workspace.GenerateGrid();
			if (grid.PartialWords.Any())
			{
				// Quick confirmation. Is there are potential word for each partial word?
				foreach(var partialWord in grid.PartialWords)
				{
					if(!workspace.WordDatabase.CanMatchWord(partialWord.Value))
					{
						yield break;
					}
				}
				foreach (var child in WorkspaceExtensions.CoverOnePartialWord(workspace, grid))
				{
					yield return child.Normalise();
				}
				yield break;
			}
			var slotEntries = workspace.SlotEntries;
			if (workspace.Board.WordPlacements.Count == 1)
			{
				var maxScore = slotEntries
					.Select(se => Scoring.Score(se.Slot.Letter))
					.Max();
				var valuableSlotes = slotEntries
					.Where(se => Scoring.Score(se.Slot.Letter) == maxScore)
					.ToList();
				foreach(var slotEntry in valuableSlotes)
				{
					foreach (var child in workspace.CoverSlot(grid, slotEntry.Slot))
					{
						yield return child.Normalise();
					}
					grid.RemoveSlot(slotEntry.Slot);
				}
				yield break;
			}

			IComparer<IEnumerable<SlotEntry>> comparer = new CompareBasedOnProximityToCorner();
			comparer = new FirstAddedSlotEntry(slotEntries);
			//comparer = new LastAddedSlotEntry(slotEntries);
			var adjacentGroups = GetAdjacentGroups(slotEntries)
				.OrderBy(
					_ => _,
					comparer
				).ToList();
			// This should be a parameter!

			foreach (var adjacentGroup in adjacentGroups)
			{
				if(adjacentGroup.Count() <= _minAdjacentGroupSize)
				{
					foreach(var slotEntry in adjacentGroup)
					{
						var slot = slotEntry.Slot;
						workspace = workspace.RemoveSlot(slot);
						foreach (var child in workspace.CoverSlot(grid, slot))
						{
							yield return child.Normalise();
						}
						grid.RemoveSlot(slot);
					}
				}
				else
				{
					// We want to start as close to an intersection
					var firstEntry = adjacentGroup.First();
					var wordPlacement = GetWordPlacement(workspace, firstEntry);
					var a = adjacentGroup;
					if(wordPlacement.Location.Equals(firstEntry.Slot.Location))
					{
						a = adjacentGroup.Reverse();
					}
					else
					{
						int dummy = 3;
					}
					foreach(var slotEntry in a.Take(_minAdjacentGroupSize + 1))
					{
						var slot = slotEntry.Slot;
						workspace = workspace.RemoveSlot(slot);
						foreach (var child in workspace.CoverSlot(grid, slot))
						{
							yield return child.Normalise();
						}
						grid.RemoveSlot(slot);
					}
					yield break;
				}
			}
		}
	}
}
