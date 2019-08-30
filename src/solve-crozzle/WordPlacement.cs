using System;

namespace solve_crozzle
{
	using crozzle;

	public class WordPlacement : IComparable<WordPlacement>
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

		public int CompareTo(WordPlacement other)
		{
			if(object.ReferenceEquals(this, other))
			{
				return 0;
			}
			int c = this.Location.CompareTo(other.Location);
			if(c != 0)
			{
				return c;
			}
			c = this.Direction.CompareTo(other.Direction);
			if(c != 0)
			{
				return c;
			}
			if(object.ReferenceEquals(this.Word, other.Word))
			{
				return 0;
			}
			c = this.Word.Length.CompareTo(other.Word.Length);
			if( c != 0)
			{
				return c;
			}
			return string.CompareOrdinal(this.Word, other.Word);
		}
	}

	public static class WordPlacementExtensions
	{
		public static WordPlacement Move(this WordPlacement wp, Vector v) =>
			new WordPlacement(wp.Direction, wp.Location + v, wp.Word);
	}
}
