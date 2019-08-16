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

		public override bool Equals(object obj)
		{
			if(object.ReferenceEquals(this, obj))
			{
				return true;
			}
			return (obj is Location l) && (l.X == X) && (l.Y == Y);
		}
	}
}
