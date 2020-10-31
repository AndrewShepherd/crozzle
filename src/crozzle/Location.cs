namespace crozzle
{
	using System;

	public class Location : IComparable<Location>
	{
		public readonly int X;
		public readonly int Y;
		public Location(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public override string ToString() =>
			$"({X}, {Y})";

		public override bool Equals(object? obj) =>
			object.ReferenceEquals(this, obj)
			||
			(
				(obj is Location l) && (l.X == X) && (l.Y == Y)
			);

		public override int GetHashCode() =>
			(X * 23 + Y) | (X * 27 + Y) << 17;

		public int CompareTo(Location? other) =>
			(other?.X) switch
			{
				null => 1,
				int x when x == X => Y.CompareTo(other.Y),
				_ => X.CompareTo(other.X)
			};

		public static bool operator !=(Location l, Location r) =>
			!(l.Equals(r));

		public static bool operator ==(Location l, Location r) =>
			l.Equals(r);

		public static Vector operator -(Location l, Location r) =>
			new Vector
			(
				l.X - r.X,
				l.Y - r.Y
			);

		public static Location operator +(Location l, Vector v) =>
			new Location
			(
				l.X + v.Dx,
				l.Y + v.Dy
			);

		public Location Offset(Direction d, int magnitude) =>
			d == Direction.Across
			? new Location(this.X + magnitude, this.Y)
			: new Location(this.X, this.Y + magnitude);

		public Location Transpose() =>
			new Location(Y, X);
	}
}
