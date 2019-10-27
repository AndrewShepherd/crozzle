using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	// I was using System.Range, but that does not allow for negative values
	class IntRange
	{
		public int Start { get; set; }
		public int EndExclusive { get; set; }

		public override string ToString() => $"{Start}..{EndExclusive - 1}";
	}

	static class IntRangeExtensions
	{
		public static bool Overlaps(this IntRange r1, IntRange r2)
		{
			if(r1.EndExclusive <= r2.Start)
			{
				return false;
			}
			if(r2.EndExclusive <= r1.Start)
			{
				return false;
			}
			return true;
		}

		public static bool IsAdjacentTo(this IntRange r1, IntRange r2) =>
			(
				(r1.EndExclusive == r2.Start)
				|| (r2.EndExclusive == r1.Start)
			);

		public static IntRange Intersection(this IntRange r1, IntRange r2)
		{
			return new IntRange
			{
				Start = Math.Max(r1.Start, r2.Start),
				EndExclusive = Math.Min(r1.EndExclusive, r2.EndExclusive),
			};
		}

		public static IntRange Union(this IntRange r1, IntRange r2) =>
			new IntRange
			{
				Start = Math.Min(r1.Start, r2.Start),
				EndExclusive = Math.Max(r1.EndExclusive, r2.EndExclusive)
			};
	}
}
	