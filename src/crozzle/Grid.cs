using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace crozzle
{
	 class Grid
	{
		internal Rectangle Rectangle { get; set; }
		internal GridCell[] Cells { get; set; }

		internal HashSet<PartialWord> PartialWords = new HashSet<PartialWord>();
	}

	internal static class GridExtensions
	{
		internal static void RemoveSlot(this Grid grid, Slot slot)
		{
			var index = grid.Rectangle.IndexOf(slot.Location);
			grid.Cells[index] = GridCell.Complete;
		}

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

		internal static GridCell CellAt(this GridCell[] gridCellArray, int index)
		{
			if((index >= 0) && (index < gridCellArray.Length))
			{
				return gridCellArray[index];
			}
			else
			{
				return new GridCell
				{
					CellType = GridCellType.Blank
				};
			}
		}

		internal static Strip GenerateStrip(this Grid grid, Direction direction, Location location)
		{
			if (direction == Direction.Across)
			{
				int indexFirst = grid.Rectangle.Right - Board.MaxWidth + 1;
				int indexLast = grid.Rectangle.Left + Board.MaxWidth - 1;
				var length = indexLast - indexFirst + 1;
				var gridCells = new GridCell[length];
				var slotIndex = location.X - indexFirst;
				gridCells[0] = GridCell.EnforcedBlank;
				gridCells[gridCells.Length - 1] = GridCell.EnforcedBlank;
				for (int i = 1; i < (gridCells.Length - 1); ++i)
				{
					var l = new Location(i + indexFirst, location.Y);
					var gridCell = grid.CellAt(l);
					gridCells[i] = gridCell;
				}
				return new Strip
				{
					GridCells = gridCells,
					StartAt = indexFirst,
					SlotIndex = slotIndex
				};
			}
			else
			{
				int indexFirst = grid.Rectangle.Bottom - Board.MaxHeight + 1;
				int indexLast = grid.Rectangle.Top + Board.MaxHeight - 1;
				var length = indexLast - indexFirst + 1;
				var gridCells = new GridCell[length];
				var slotIndex = location.Y - indexFirst;
				gridCells[0] = GridCell.EnforcedBlank;
				gridCells[gridCells.Length - 1] = GridCell.EnforcedBlank;
				for (int i = 1; i < (gridCells.Length - 1); ++i)
				{
					var l = new Location(location.X, indexFirst + i);
					var gridCell = grid.CellAt(l);
					gridCells[i] = gridCell;
				}
				var strip = new Strip
				{
					GridCells = gridCells,
					StartAt = indexFirst,
					SlotIndex = slotIndex
				};


				// Find the last dollar sign before the slotIndex
				int indexOfFirstDollarBeforeSlotIndex = slotIndex - 1;
				for (;
					indexOfFirstDollarBeforeSlotIndex >= 0
					&&
					gridCells[indexOfFirstDollarBeforeSlotIndex].CellType != GridCellType.Complete;
					--indexOfFirstDollarBeforeSlotIndex
				) ;
				if (indexOfFirstDollarBeforeSlotIndex != -1)
				{
					strip = new Strip
					{
						GridCells = strip.GridCells.Skip(indexOfFirstDollarBeforeSlotIndex + 1).ToArray(),
						SlotIndex = strip.SlotIndex - (indexOfFirstDollarBeforeSlotIndex + 1),
						StartAt = strip.StartAt + indexOfFirstDollarBeforeSlotIndex + 1
					};
				}
				return strip;
			}
		}
	}

}
