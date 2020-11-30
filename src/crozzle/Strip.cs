namespace crozzle
{
	internal record Strip
	{
		public GridCell[] GridCells { get; init; }
		public int StartAt { get; init; }
		public int SlotIndex { get; internal init; }
	}
}
