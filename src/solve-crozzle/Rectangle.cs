using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public class Rectangle
	{
		public int MinX;
		public int MaxX;
		public int MinY;
		public int MaxY;

		public int Width => MaxX - MinX + 1;
		public int Height => MaxY - MinY + 1;

		public static Rectangle Union(Rectangle r1, Rectangle r2) =>
			new Rectangle
			{
				MinX = Math.Min(r1.MinX, r2.MinX),
				MaxX = Math.Max(r1.MaxX, r2.MaxX),
				MinY = Math.Min(r1.MinY, r2.MinY),
				MaxY = Math.Max(r1.MaxY, r2.MaxY)
			};

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj))
				return true;
			var other = obj as Rectangle;
			if (object.ReferenceEquals(other, null))
				return false;
			return this.MinX == other.MinX
				&& this.MaxX == other.MaxX
				&& this.MinY == other.MinY
				&& this.MaxY == other.MaxY;
		}

	}

	public static class RectangleExtensions
	{
		public static Rectangle Union(this Rectangle r1, Rectangle r2) =>
			Rectangle.Union(r1, r2);
	}
}
