namespace crozzle
{
	using System;

	public class Slot : IComparable<Slot>
	{

		private readonly Direction _direction;
		private readonly char _letter;
		private readonly Location _location;
		public Direction Direction => _direction;
		public char Letter => _letter;
		public Location Location => _location;

		public override string ToString()
			=> $"{Location} {Direction}: {Letter}";

		public Slot(char letter, Direction direction, Location location)
		{
			_direction = direction;
			_letter = letter;
			_location = location;
		}

		public override bool Equals(object? obj)
		{
			if (!(obj is Slot s))
			{
				return false;
			}
			return Direction.Equals(s.Direction) 
				&& Letter.Equals(s.Letter) 
				&& Location.Equals(s.Location);
		}

		public override int GetHashCode() =>
			Direction.GetHashCode()
				^ ((int)Letter << 4)
				^ Location.GetHashCode();

		public Slot Move(Vector v) =>
			new Slot
			(
				this.Letter,
				this.Direction,
				this.Location + v
			);

		public int CompareTo(Slot? other)
		{
			int locationComparison = this.Location.CompareTo(other.Location);
			if (locationComparison != 0)
			{
				return locationComparison;
			}
			var directionComparison = this.Direction.CompareTo(other.Direction);
			if (directionComparison != 0)
			{
				return directionComparison;
			}
			return this.Letter.CompareTo(other.Letter);
		}
	}

	public static class SlotExtensions
	{
		public static Slot Move(this Slot slot, Vector v) =>
			new Slot(slot.Letter, slot.Direction, slot.Location + v);
	}

}
