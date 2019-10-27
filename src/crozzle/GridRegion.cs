using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace crozzle
{
	 class GridRegion
	{
		internal IEnumerable<RowIndexAndRange> RowIndexAndRanges;
	}

	static class GridRegionExtensions
	{
		internal static int CountLocations(this IEnumerable<RowIndexAndRange> e) =>
			e.Select(rr => rr.Range).Sum(r => r.EndExclusive - r.Start);

		internal static bool IsAdjacentTo(this RowIndexAndRange r1, RowIndexAndRange r2)
		{
			if(r1.RowIndex == r2.RowIndex)
			{
				return r1.Range.IsAdjacentTo(r2.Range);
			}
			if(Math.Abs(r1.RowIndex - r2.RowIndex) == 1)
			{
				return r1.Range.Overlaps(r2.Range);
			}
			return false;
		}

		internal static int CountLocations(this GridRegion gridRegion)
			=> CountLocations(gridRegion.RowIndexAndRanges);

		internal static GridRegion Intersection(this GridRegion r1, GridRegion r2)
		{
			List<RowIndexAndRange> list = new List<RowIndexAndRange>();
			foreach(var rr1 in r1.RowIndexAndRanges)
			{
				foreach(var rr2 in r2.RowIndexAndRanges)
				{
					if(rr1.RowIndex == rr2.RowIndex)
					{
						if(rr1.Range.Overlaps(rr2.Range))
						{
							list.Add(
								new RowIndexAndRange
								{
									RowIndex = rr1.RowIndex,
									Range = rr1.Range.Intersection(rr2.Range)
								}
							);
						}
					}
				}
			}
			return new GridRegion
			{
				RowIndexAndRanges = list,
			};
		}

		private static IEnumerable<IntRange> MergeAdjacentRanges(List<IntRange> ranges)
		{
			while(ranges.Count > 0)
			{
				var range = ranges.ElementAt(0);
				ranges.RemoveAt(0);
				for(int i = ranges.Count() - 1; i >= 0; --i)
				{
					if(ranges[i].IsAdjacentTo(range))
					{
						range = range.Union(ranges[i]);
						ranges.RemoveAt(i);
					}
				}
				yield return range;
			}
		}

		internal static GridRegion Union(this GridRegion r1, GridRegion r2)
		{
			var groups = r1.RowIndexAndRanges
				.Concat(r2.RowIndexAndRanges)
				.GroupBy(r => r.RowIndex)
				.ToList();
			var merged = groups
				.Select(
					g =>
					{
						return new
						{
							RowIndex = g.Key,
							Ranges = MergeAdjacentRanges(
								g.Select(r3 => r3.Range).ToList()
							),
						};
					}
			);
			return new GridRegion
			{
				RowIndexAndRanges = merged
					.SelectMany(
						m =>
							m.Ranges.Select(r => new RowIndexAndRange { RowIndex = m.RowIndex, Range = r })
					).ToList()
			};
		}

		internal static bool IsAdjacentTo(this GridRegion gridRegion, GridRegion other) =>
			gridRegion.RowIndexAndRanges.Any(
				r =>
					other
						.RowIndexAndRanges
						.Any(r2 => r.IsAdjacentTo(r2))
			);

		internal static bool OverlapsWith(this GridRegion gridRegion, GridRegion other) =>
			gridRegion.RowIndexAndRanges.Any(
				r =>
					other
						.RowIndexAndRanges
						.Any(
							r2 => ((r2.RowIndex == r.RowIndex) && (r2.Range.Overlaps(r.Range)))
						)
			);

		internal static bool OverlapsWith(this RowIndexAndRange rowIndexAndRange, Rectangle rectangle)
		{
			if(rowIndexAndRange.RowIndex < rectangle.Top)
			{
				return false;
			}
			if(rowIndexAndRange.RowIndex > rectangle.Bottom)
			{
				return false;
			}
			if(rowIndexAndRange.Range.Start > rectangle.Right)
			{
				return false;
			}
			if(rowIndexAndRange.Range.EndExclusive <= rectangle.Left)
			{
				return false;
			}
			return true;
		}

		internal static bool OverlapsWith(this GridRegion gridRegion, Rectangle rectangle) =>
			gridRegion.RowIndexAndRanges.Any(
				r => r.OverlapsWith(rectangle)
			);
	}
}
