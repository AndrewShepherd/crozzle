namespace crozzle
{
	using System;

	public sealed record Location(int X, int Y) : IComparable<Location>
	{
		public int CompareTo(Location? other)
		{
			return other switch
			{
				null => 1,
				Location l when l.X == this.X  => this.Y.CompareTo(l.Y),
				_ => this.X.CompareTo(other.X),
			};
		}

		public override string ToString() =>
			$"({X}, {Y})";

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
