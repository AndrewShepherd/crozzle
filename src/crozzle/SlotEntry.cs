using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace crozzle
{
	public class SlotEntry : IComparable<SlotEntry>
	{
		public Slot Slot { get; set; }
		public ImmutableHashSet<CandidateWord> CandidateWords = ImmutableHashSet<CandidateWord>.Empty;

		public int CompareTo(SlotEntry other)
		{
			return this.Slot.CompareTo(other.Slot);
		}
	}

	public static class SlotEntryExtensions
	{
		public static SlotEntry Move(this SlotEntry slotEntry, Vector diff) =>
			new SlotEntry
			{
				Slot = slotEntry.Slot.Move(diff),
				CandidateWords = slotEntry.CandidateWords
			};

		public static SlotEntry RemoveCandidateWords(
			this SlotEntry slotEntry,
			IEnumerable<CandidateWord> candidateWords
		) =>
			new SlotEntry
			{
				Slot = slotEntry.Slot,
				CandidateWords = slotEntry.CandidateWords.Except(candidateWords),
			};

		public static ImmutableList<SlotEntry> Remove(this ImmutableList<SlotEntry> slotEntries, Slot slot)
		{
			var entry = slotEntries.FirstOrDefault(se => se.Slot.Equals(slot));
			return entry == null ? slotEntries : slotEntries.Remove(entry);
		}

		public static ImmutableList<SlotEntry> Replace(this ImmutableList<SlotEntry> slotEntries, SlotEntry slotEntry)
		{
			var oldEntry = slotEntries.Where(se => se.Slot.Equals(slotEntry.Slot)).FirstOrDefault();
			return oldEntry == null ? slotEntries : slotEntries.Replace(oldEntry, slotEntry);
		}

		public static ImmutableList<SlotEntry> RemoveCandidateWords(this ImmutableList<SlotEntry> slotEntries, SlotEntry slotEntry, IEnumerable<CandidateWord> candidateWords)
		{
			var entry = slotEntries.FirstOrDefault(se => se.Slot.Equals(slotEntry.Slot));
			if(entry == null)
			{
				return slotEntries;
			}
			var newEntry = entry.RemoveCandidateWords(candidateWords);
			return newEntry.CandidateWords.Any()
				? slotEntries.Replace(entry, newEntry)
				: slotEntries.Remove(entry);
		}

		static ImmutableHashSet<CandidateWord> SafeIntersect(this ImmutableHashSet<CandidateWord> c1, ImmutableHashSet<CandidateWord> c2)
		{
			if(c1 == null)
			{
				return c2;
			}
			if(c2 == null)
			{
				return c1;
			}
			return c1.Intersect(c2);
		}

		public static ImmutableList<SlotEntry> IntersectWith(this ImmutableList<SlotEntry> slotEntries, ImmutableList<SlotEntry> slotEntries2)
		{
			foreach(var se2 in slotEntries2)
			{
				var matching = slotEntries.Where(se => se.Slot.Equals(se2.Slot)).FirstOrDefault();
				if(matching == null)
				{
					continue;
				}
				slotEntries = slotEntries.Replace(
					matching,
					new SlotEntry
					{
						Slot = matching.Slot,
						CandidateWords = matching.CandidateWords.SafeIntersect(se2.CandidateWords)
					}
				);
			}
			return slotEntries;
		}
	}
}
