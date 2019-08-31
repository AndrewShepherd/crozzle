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

	}
}
