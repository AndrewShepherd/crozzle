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
						if(rr1.Range.Intersects(rr2.Range))
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

		internal static bool OverlapsWith(this GridRegion gridRegion, GridRegion other) =>
			gridRegion.RowIndexAndRanges.Any(
				r =>
					other
						.RowIndexAndRanges
						.Any(
							r2 => ((r2.RowIndex == r.RowIndex) && (r2.Range.Intersects(r.Range)))
						)
			);
	}
}
