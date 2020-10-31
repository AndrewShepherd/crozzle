using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	enum GridCellType {
		Blank, // Currently Blank, can be filled
		EndOfWordMarker, // Currently Blank, must remain blank
		BlankNoAdjacentSlots, // Currently blank because there is an adjacent cell that is not a slot
		AvailableSlot, // Has a letter, can be intersected with
		Complete // Has a letter, but cannot be intersected with
	};
	class GridCell
	{
		public override string ToString() => CellType.ToString();
		public GridCellType CellType { get; set; } = GridCellType.Blank;

		public SlotEntry? SlotEntry { get; set; }
		public Slot? Slot => SlotEntry?.Slot;
		public char? Letter => WordAndIndex?.Word[WordAndIndex?.Index ?? 0];

		public WordAndIndex? WordAndIndex { get; internal set; }

		internal static GridCell EnforcedBlank = new GridCell
		{ 
			CellType = GridCellType.EndOfWordMarker
		};

		internal PartialWord? PartialWordAbove { get; set; }
		internal PartialWord? PartialWordBelow { get; set; }
		internal PartialWord? PartialWordToLeft { get; set; }
		internal PartialWord? PartialWordToRight { get; set; }

		internal static GridCell FromSlot(SlotEntry slotEntry) =>
			new GridCell
			{
				CellType = GridCellType.AvailableSlot,
				SlotEntry = slotEntry,
			};

		internal static GridCell Complete = new GridCell
		{
			CellType = GridCellType.Complete
		};

		internal bool HasLetter => CellType == GridCellType.AvailableSlot || CellType == GridCellType.Complete;


		internal bool CanPlaceEndOfWordMarker =>
			CellType == GridCellType.EndOfWordMarker
			|| CellType == GridCellType.Blank
			|| CellType == GridCellType.BlankNoAdjacentSlots;

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
