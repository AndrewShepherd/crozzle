using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	internal class Strip
	{
		public GridCell[] GridCells;
		public int StartAt;
		public int SlotIndex { get; internal set; }
	}
}
