using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public class Rectangle
	{
		public readonly Location TopLeft;
		public readonly int Width;
		public readonly int Height;

		public Rectangle(Location topLeft, int width, int height)
		{
			TopLeft = topLeft;
			Width = width;
			Height = height;
		}

		public static Rectangle Union(Rectangle r1, Rectangle r2)
		{
			var topLeft = new Location(
				Math.Min(r1.TopLeft.X, r2.TopLeft.X),
				Math.Min(r1.TopLeft.Y, r2.TopLeft.Y)
			);
			var bottomRight = new Location(
				Math.Max(r1.TopLeft.X + r1.Width - 1, r2.TopLeft.X + r2.Width - 1),
				Math.Max(r1.TopLeft.Y + r1.Height -1, r2.TopLeft.Y + r2.Height - 1)
			);
			return new Rectangle(
				topLeft,
				bottomRight.X - topLeft.X + 1,
				bottomRight.Y - topLeft.Y + 1
			);
		}

		public int Area => Width * Height;

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj))
				return true;
			var other = obj as Rectangle;
			if (object.ReferenceEquals(other, null))
				return false;
			return this.TopLeft.Equals(other.TopLeft)
				&& this.Width == other.Width
				&& this.Height == other.Height;
		}

		public override int GetHashCode() =>
			this.TopLeft.GetHashCode()
			^ this.Width
			^ this.Height;
	}

	public static class RectangleExtensions
	{
		public static Rectangle Union(this Rectangle r1, Rectangle r2) =>
			Rectangle.Union(r1, r2);
	}
}
