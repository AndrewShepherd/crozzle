using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	enum GridCellType {
		Blank, // Currently Blank, can be filled
		EnforcedBlank, // Currently Blank, must remain blank
		AvailableSlot, // Has a letter, can be intersected with
		Complete // Has a letter, but cannot be intersected with
	};
	class GridCell
	{
		public GridCellType CellType { get; set; } = GridCellType.Blank;
		public Slot Slot { get; set; }
		public char Letter { get; internal set; }

		internal static GridCell EnforcedBlank = new GridCell
		{ 
			CellType = GridCellType.EnforcedBlank
		};

		internal PartialWord PartialWordAbove { get; set; }
		internal PartialWord PartialWordBelow { get; set; }
		internal PartialWord PartialWordToLeft { get; set; }
		internal PartialWord PartialWordToRight { get; set; }

		internal static GridCell FromSlot(Slot slot) =>
			new GridCell
			{
				CellType = GridCellType.AvailableSlot,
				Slot = slot
			};

		internal static GridCell Complete = new GridCell
		{
			CellType = GridCellType.Complete
		};

		internal bool HasLetter => CellType == GridCellType.AvailableSlot || CellType == GridCellType.Complete;

	}

	static class GridCellExtensions
	{
		internal static PartialWord PredictPartialWordToBeCreated(this GridCell gridCell, Location l, char placedChar, Direction direction)
		{
			PartialWord partialWord;
			if (direction == Direction.Across)
			{
				partialWord = new PartialWord
				{
					Direction = Direction.Down,
					Rectangle = new Rectangle(l, l),
					Value = placedChar.ToString()
				};
				if (gridCell.PartialWordAbove != null)
				{
					partialWord = new PartialWord
					{
						Direction = Direction.Down,
						Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordAbove.Rectangle),
						Value = $"{gridCell.PartialWordAbove.Value}{partialWord.Value}",
					};
				}
				if (gridCell.PartialWordBelow != null)
				{
					partialWord = new PartialWord
					{
						Direction = Direction.Down,
						Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordBelow.Rectangle),
						Value = $"{partialWord.Value}{gridCell.PartialWordBelow.Value}",
					};
				}
			}
			else
			{
				partialWord = new PartialWord
				{
					Direction = Direction.Across,
					Rectangle = new Rectangle(l, l),
					Value = placedChar.ToString()
				};
				if (gridCell.PartialWordToLeft != null)
				{
					partialWord = new PartialWord
					{
						Direction = Direction.Across,
						Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordToLeft.Rectangle),
						Value = $"{gridCell.PartialWordToLeft.Value}{partialWord.Value}",
					};
				}
				if (gridCell.PartialWordToRight != null)
				{
					partialWord = new PartialWord
					{
						Direction = Direction.Across,
						Rectangle = partialWord.Rectangle.Union(gridCell.PartialWordToRight.Rectangle),
						Value = $"{partialWord.Value}{gridCell.PartialWordToRight.Value}",
					};
				}
			}

			return partialWord;
		}

	}
}
