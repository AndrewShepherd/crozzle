﻿using System;
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
	}
}
