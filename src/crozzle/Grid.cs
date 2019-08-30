using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	 class Grid
	{
		internal Rectangle Rectangle { get; set; }
		internal GridCell[] Cells { get; set; }
	}


	internal static class GridExtensions
	{
		internal static GridCell CellAt(this Grid grid, Location location)
		{
			if (!(grid.Rectangle.Contains(location)))
			{
				return new GridCell
				{
					CellType = GridCellType.Blank
				};
			}
			int index = grid.Rectangle.IndexOf(location);
			return grid.Cells[index];
		}


		internal static bool CanPlaceWord(this Grid grid, Direction direction, string word, Location location)
		{
			var wordPlacement = new WordPlacement(direction, location, word);
			var r = wordPlacement.GetRectangle()
				.Union(grid.Rectangle);
			if ((r.Width > Board.MaxWidth) || (r.Height > Board.MaxHeight))
				return false;
			(
				var startMarkerLocation,
				var endMarkerLocation
			) = direction == Direction.Across
			? (new Location(location.X - 1, location.Y), new Location(location.X + word.Length, location.Y))
			: (new Location(location.X, location.Y - 1), new Location(location.X, location.Y + word.Length));

			var startMarker = grid.CellAt(startMarkerLocation);
			if((startMarker.CellType != GridCellType.Blank) && (startMarker.CellType != GridCellType.EnforcedBlank))
			{
				return false;
			}
			var endMarker = grid.CellAt(endMarkerLocation);
			if ((endMarker.CellType != GridCellType.Blank) && (endMarker.CellType != GridCellType.EnforcedBlank))
			{
				return false;
			}
			for (int i = 0; i < word.Length; ++i)
			{

				var l = direction == Direction.Down
					? new Location(location.X, location.Y + i)
					: new Location(location.X + i, location.Y);
				var c = grid.CellAt(l);
				if(c.CellType == GridCellType.Blank)
				{
					continue;
				}
				if((c.CellType == GridCellType.AvailableSlot) && c.Slot.Letter == word[i])
				{
					continue;
				}
				return false;
			}
			return true;
		}
	}

}
