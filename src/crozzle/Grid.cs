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

	class RowIndexAndRange
	{
		public int RowIndex;
		public IntRange Range;

		public IEnumerable<Location> GetLocations()
		{
			for (int i = Range.Start; i < Range.EndExclusive; ++i)
			{
				yield return new Location(i, RowIndex);
			}
		}

		public override string ToString() => $"{RowIndex}: {Range?.ToString()}";
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
			if(grid.Rectangle.Contains(location))
			{
				int index = grid.Rectangle.IndexOf(location);
				return grid.Cells[index];
			}
			else
			{
				PartialWord partialWordAbove = null;
				PartialWord partialWordBelow = null;
				PartialWord partialWordToLeft = null;
				PartialWord partialWordToRight = null;

				Location locationAbove = new Location(location.X, location.Y - 1);
				if(grid.Rectangle.Contains(locationAbove))
				{
					var cellAbove = grid.CellAt(locationAbove);
					if(cellAbove.CellType == GridCellType.Complete || cellAbove.CellType == GridCellType.AvailableSlot)
					{
						var furtherPartialWord = cellAbove.PartialWordAbove;
						partialWordAbove = new PartialWord
						{
							Direction = Direction.Down,
							Rectangle = new Rectangle(
								furtherPartialWord?.Rectangle?.TopLeft ?? locationAbove,
								locationAbove
							),
							Value = $"{furtherPartialWord?.Value ?? string.Empty}{cellAbove.Letter}"
						};
					}
				}
				
				// This is incomplete information
				// It does not mention partial words
				return new GridCell
				{
					CellType = GridCellType.Blank,
					PartialWordAbove = partialWordAbove
				};
			}

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

		static internal IEnumerable<RowIndexAndRange> GetContiguousRangesForEachRow(this Grid grid, Func<GridCell, bool> matchingCell)
		{
			for (int row = grid.Rectangle.Top + 1; row <= grid.Rectangle.Bottom - 1; ++row)
			{
				var emptyCellsInRow = new List<IntRange>();
				for (
					Location l = new Location(grid.Rectangle.Left+1, row);
					l.X <= grid.Rectangle.Right-1;
					l = new Location(l.X + 1, l.Y)
				)
				{
					var cell = grid.CellAt(l);
					if (matchingCell(cell))
					{
						emptyCellsInRow.Add(
							new IntRange
							{
								Start = l.X,
								EndExclusive = l.X + 1
							}
						);
					}
				}
				if (emptyCellsInRow.Any())
				{
					var contiguousSpan = emptyCellsInRow.First();
					foreach (var l in emptyCellsInRow.Skip(1))
					{
						if (l.Start.Equals(contiguousSpan.EndExclusive))
						{
							contiguousSpan = new IntRange
							{
								Start = contiguousSpan.Start,
								EndExclusive = l.EndExclusive
							};
						}
						else
						{
							yield return new RowIndexAndRange
							{
								RowIndex = row,
								Range = contiguousSpan
							};
							contiguousSpan = new IntRange
							{
								Start = l.Start,
								EndExclusive = l.EndExclusive
							}; 
						}
					}
					yield return new RowIndexAndRange
					{
						RowIndex = row,
						Range = contiguousSpan
					};
				}
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
					if(gridCell.CellType == GridCellType.BlankNoAdjacentSlots)
					{
						gridCell = GridCell.EnforcedBlank;
					}
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

		internal static IEnumerable<GridRegion> FindEnclosedSpaces(this Grid grid, Func<GridCell, bool> cellMatches)
		{
			var rowIndexAndRanges = grid.GetContiguousRangesForEachRow(cellMatches);

			var nodes = rowIndexAndRanges.ToArray();
			bool[,] connections = new bool[nodes.Length, nodes.Length];
			for (int i = 0; i < nodes.Length - 1; ++i)
			{
				var node = nodes[i];
				for (int j = i + 1; j < nodes.Length; ++j)
				{
					var node2 = nodes[j];
					if (
						(Math.Abs(node.RowIndex - node2.RowIndex) == 1)
						&& (node.Range.Overlaps(node2.Range))
					)
					{
						connections[i, j] = true;
						connections[j, i] = true;
					}
				}
			}
			for (int i = 0; i < nodes.Length; ++i)
			{
				if (nodes[i] != null)
				{
					List<RowIndexAndRange> space = new List<RowIndexAndRange>();
					var bag = new Stack<int>();
					bag.Push(i);
					space.Add(nodes[i]);
					nodes[i] = null;
					while (bag.Any())
					{
						var node = bag.Pop();
						for (int j = i + 1; j < nodes.Length; ++j)
						{
							if ((nodes[j] != null) && (connections[node, j]))
							{
								space.Add(nodes[j]);
								nodes[j] = null;
								bag.Push(j);
							}
						}
					}
					yield return new GridRegion
					{
						RowIndexAndRanges = space
					};
				}
			}
		}


	}

}
