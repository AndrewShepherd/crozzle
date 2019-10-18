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
			e.Select(rr => rr.Range).Sum(r => r.End.Value - r.Start.Value);

		internal static int CountLocations(this GridRegion gridRegion)
			=> CountLocations(gridRegion.RowIndexAndRanges);
	}
}
