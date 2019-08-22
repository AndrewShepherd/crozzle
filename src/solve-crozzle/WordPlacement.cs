namespace solve_crozzle
{
	public class WordPlacement
	{
		public Location Location { get; private set; }
		public string Word { get; private set; }
		public Direction Direction { get; private set; }

		public override string ToString()
			=> $"{Location} {Direction}: {Word}";

		public WordPlacement(Direction direction, Location location, string Word)
		{
			this.Direction = direction;
			this.Location = location;
			this.Word = Word;
		}

		public override bool Equals(object obj) =>
			object.ReferenceEquals(this, obj)
			|| (
				(obj is WordPlacement w)
					&& w.Direction.Equals(this.Direction)
					&& w.Location.Equals(this.Location)
					&& w.Word.Equals(this.Word)
			);

			public override int GetHashCode()
			{
				unchecked
				{
					return Location.GetHashCode()
						^ Word.GetHashCode()
						^ (
							Direction == Direction.Across
								? (int)0xC0C0C0C0
								: 0x0C0C0C0C
						);
				}
			}
	}

	public static class WordPlacementExtensions
	{
		public static WordPlacement Move(this WordPlacement wp, Vector v) =>
			new WordPlacement(wp.Direction, wp.Location + v, wp.Word);
	}
}
