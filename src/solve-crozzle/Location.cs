using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public class Location
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
			(X << 16) ^ Y;

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
