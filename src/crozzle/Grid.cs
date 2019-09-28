﻿using System;
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

	class RowIndexAndRanges
	{
		public int RowIndex;
		public List<Range> Ranges;

		public IEnumerable<Location> GetLocations()
		{
			foreach(var r in Ranges)
			{
				for(int i = r.Start.Value; i < r.End.Value; ++i)
				{
					yield return new Location(i, RowIndex);
				}
			}
		}
	}

	internal static class GridExtensions
	{
		internal static int CountLocations(this IEnumerable<RowIndexAndRanges> e) =>
			e.SelectMany(rr => rr.Ranges).Sum(r => r.End.Value - r.Start.Value);

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

		static internal IEnumerable<RowIndexAndRanges> GetContiguousRangesForEachRow(this Grid grid)
		{
			for (int row = grid.Rectangle.Top + 1; row <= grid.Rectangle.Bottom - 1; ++row)
			{
				List<Range> emptyCellsInRow = new List<Range>();
				for (
					Location l = new Location(grid.Rectangle.Left+1, row);
					l.X <= grid.Rectangle.Right-1;
					l = new Location(l.X + 1, l.Y)
				)
				{
					var cell = grid.CellAt(l);
					if ((cell.CellType == GridCellType.Blank) || (cell.CellType == GridCellType.EnforcedBlank))
					{
						emptyCellsInRow.Add(new Range(l.X, l.X + 1));
					}
				}
				List<Range> emptySpans = new List<Range>();
				if (emptyCellsInRow.Any())
				{
					Range contiguousSpan = emptyCellsInRow.First();
					foreach (var l in emptyCellsInRow.Skip(1))
					{
						if (l.Start.Equals(contiguousSpan.End))
						{
							contiguousSpan = new Range(contiguousSpan.Start.Value, l.End.Value);
						}
						else
						{
							emptySpans.Add(contiguousSpan);
							contiguousSpan = new Range(l.Start, l.End);
						}
					}
					emptySpans.Add(contiguousSpan);
				}
				yield return new RowIndexAndRanges
				{
					RowIndex = row,
					Ranges = emptySpans
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

		internal static bool Intersects(Range r1, Range r2)
		{
			if (r1.End.Value <= r2.Start.Value)
			{
				return false;
			}
			if (r2.End.Value <= r1.Start.Value)
			{
				return false;
			}
			return true;
		}

		internal static IEnumerable<List<RowIndexAndRanges>> GenerateSpaces(this Grid grid)
		{
			var rowIndexAndRanges = grid.GetContiguousRangesForEachRow();

			var nodes = rowIndexAndRanges.SelectMany(
				r =>
					r.Ranges.Select(
						rr =>
							new RowIndexAndRanges
							{
								RowIndex = r.RowIndex,
								Ranges = new List<Range>() { rr }
							}
					)
			).ToArray();
			bool[,] connections = new bool[nodes.Length, nodes.Length];
			for (int i = 0; i < nodes.Length - 1; ++i)
			{
				var node = nodes[i];
				for (int j = i + 1; j < nodes.Length; ++j)
				{
					var node2 = nodes[j];
					if (
						(Math.Abs(node.RowIndex - node2.RowIndex) == 1)
						&& (Intersects(node.Ranges[0], node2.Ranges[0]))
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
					List<RowIndexAndRanges> space = new List<RowIndexAndRanges>();
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
					yield return space;
				}
			}
		}


	}

}
