﻿namespace crozzle
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

		public override bool Equals(object obj) =>
			object.ReferenceEquals(this, obj)
			||
			(
				(obj is Location l) && (l.X == X) && (l.Y == Y)
			);

		public override int GetHashCode() =>
			(X * 23 + Y) | (X * 27 + Y) << 17;

		public int CompareTo(Location other) =>
			X == other.X
			? Y.CompareTo(other.Y)
			: X.CompareTo(other.X);

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
	}
}