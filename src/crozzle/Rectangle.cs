namespace crozzle
{
	using System;

	public record Rectangle
	{
		public Location TopLeft { get; init; }
		public int Width { get; init; }
		public int Height { get; init; }

		public int Left => TopLeft.X;
		public int Top => TopLeft.Y;
		public int Right => TopLeft.X + Width - 1;
		public int Bottom => TopLeft.Y + Height - 1;

		public Rectangle(Location topLeft, int width, int height)
		{
			TopLeft = topLeft;
			Width = width;
			Height = height;
		}

		public Rectangle(Location topLeft, Location bottomRight)
		{
			TopLeft = topLeft;
			Width = bottomRight.X - topLeft.X + 1;
			Height = bottomRight.Y - topLeft.Y + 1;
		}

		private static Rectangle _empty = new Rectangle(new Location(0, 0), 0, 0);

		public static Rectangle Empty => _empty;

		public bool Contains(Location location)
		{
			if (location.X < this.TopLeft.X)
				return false;
			if (location.Y < this.TopLeft.Y)
				return false;
			if (location.X > this.TopLeft.X + this.Width - 1)
				return false;
			if (location.Y > this.TopLeft.Y + this.Height - 1)
				return false;
			return true;
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

		public override string ToString() =>
			$"({TopLeft}), {Width}x{Height}";

	}

	enum TraversalDirection { LeftToRight, RightToLeft, UpToDown, DownToUp };

	public static class RectangleExtensions
	{
		public static Rectangle Union(this Rectangle r1, Rectangle r2) =>
			Rectangle.Union(r1, r2);

		public static Location CalculateLocation(this Rectangle rectangle, int index) =>
			rectangle.Width == 0
			? new Location(0, 0)
			: new Location(
				index % rectangle.Width + rectangle.TopLeft.X,
				index / rectangle.Width + rectangle.TopLeft.Y
			);

		public static int CalculateIndex(int width, int xStart, int yStart, Location location) =>
			(location.Y - yStart) * width + (location.X - xStart);

		public static int IndexOf(this Rectangle rectangle, Location location) =>
			CalculateIndex(
				rectangle.Width,
				rectangle.TopLeft.X,
				rectangle.TopLeft.Y,
				location
			);

		public static bool IntersectsWith(this Rectangle r1, Rectangle r2)
		{
			if (r1.TopLeft.X > (r2.TopLeft.X + r2.Width - 1))
				return false;
			if (r2.TopLeft.X > (r1.TopLeft.X + r1.Width - 1))
				return false;
			if (r1.TopLeft.Y > (r2.TopLeft.Y + r2.Height - 1))
				return false;
			if (r2.TopLeft.Y > (r1.TopLeft.Y + r1.Height - 1))
				return false;
			return true;
		}

		public static Rectangle Move(this Rectangle r, Vector v) =>
			r with { TopLeft = r.TopLeft + v };
	}
}
