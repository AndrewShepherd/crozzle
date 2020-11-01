using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace crozzle
{
	public class SlotEntry : IComparable<SlotEntry>
	{
		public Slot Slot { get; private set; }
		public ImmutableHashSet<WordAndIndex>? CandidateWords
		{
			get;
			private set;
		}

		public SlotEntry(Slot slot, ImmutableHashSet<WordAndIndex>? candidateWords)
		{
			this.Slot = slot;
			this.CandidateWords = candidateWords;
		}

		public int CompareTo(SlotEntry? other) =>
			other switch
			{
				null => 1,
				_ => this.Slot.CompareTo(other.Slot)
			};

		public override int GetHashCode()
		{
			return this.Slot.GetHashCode()
				^ (
					(CandidateWords == null)
						? 0
						: HashUtils.GenerateHash(CandidateWords)
				);
		}

		public override bool Equals(object? obj)
		{
			if(object.ReferenceEquals(this, obj))
			{
				return true;
			}
			if(!(obj is SlotEntry other))
			{
				return false;
			}
			if(!(this.Slot.Equals(other.Slot)))
			{
				return false;
			}
			if(object.ReferenceEquals(this.CandidateWords, other.CandidateWords))
			{
				return true;
			}
			if(object.ReferenceEquals(this.CandidateWords, null))
			{
				return false;
			}
			if(object.ReferenceEquals(other.CandidateWords, null))
			{
				return false;
			}
			if(!(this.CandidateWords.Count.Equals(other.CandidateWords.Count)))
			{
				return false;
			}
			if(!Enumerable.SequenceEqual(this.CandidateWords, other.CandidateWords))
			{
				return false;
			}
			return true;
		}
	}

	public static class SlotEntryExtensions
	{
		public static SlotEntry Move(this SlotEntry slotEntry, Vector diff) =>
			new SlotEntry
			(
				slotEntry.Slot.Move(diff),
				slotEntry.CandidateWords
			);

		public static SlotEntry RemoveCandidateWords(
			this SlotEntry slotEntry,
			IEnumerable<WordAndIndex> candidateWords
		) =>
			new SlotEntry
			(
				slotEntry.Slot,
				slotEntry.CandidateWords?.Except(candidateWords)
			);

		public static ImmutableList<SlotEntry> Remove(this ImmutableList<SlotEntry> slotEntries, Slot slot)
		{
			for(int i = 0; i < slotEntries.Count; ++i)
			{
				if(slotEntries[i].Slot.Equals(slot))
				{
					return slotEntries.RemoveAt(i);
				}
			}
			return slotEntries;
		}

		public static ImmutableList<SlotEntry> Replace(this ImmutableList<SlotEntry> slotEntries, SlotEntry slotEntry)
		{
			var oldEntry = slotEntries.Where(se => se.Slot.Equals(slotEntry.Slot)).FirstOrDefault();
			return oldEntry == null ? slotEntries : slotEntries.Replace(oldEntry, slotEntry);
		}

		public static ImmutableList<SlotEntry> RemoveCandidateWords(this ImmutableList<SlotEntry> slotEntries, SlotEntry slotEntry, IEnumerable<WordAndIndex> candidateWords)
		{
			var entry = slotEntries.FirstOrDefault(se => se.Slot.Equals(slotEntry.Slot));
			if(entry == null)
			{
				return slotEntries;
			}
			var newEntry = entry.RemoveCandidateWords(candidateWords);
			return slotEntries.Replace(entry, newEntry);
		}

		static ImmutableHashSet<WordAndIndex>? SafeIntersect(this ImmutableHashSet<WordAndIndex>? c1, ImmutableHashSet<WordAndIndex>? c2)
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
					(
						matching.Slot,
						matching.CandidateWords
							.SafeIntersect(se2.CandidateWords)
					)
				);
			}
			return slotEntries;
		}
	}
}
