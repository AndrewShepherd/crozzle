﻿namespace crozzle
{
	using System;

	public class Rectangle
	{
		public readonly Location TopLeft;
		public readonly int Width;
		public readonly int Height;

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

		public override bool Equals(object? obj)
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

	enum TraversalDirection { LeftToRight, RightToLeft, UpToDown, DownToUp };

	public static class RectangleExtensions
	{
		internal static void Traverse(
			this Rectangle rectangle,
			TraversalDirection traversalDirection,
			Action handleLineStart,
			Action<Location> handleCell
		)
		{
			(
				Location lineBeginning,
				Location lineEnd,
				Location terminationPoint,
				Vector lineIncrement,
				Vector cellIncrement
			) = traversalDirection switch
			{
				TraversalDirection.LeftToRight =>
				(
					rectangle.TopLeft,
					new Location(rectangle.Right + 1, rectangle.Top),
					new Location(rectangle.Left, rectangle.Bottom + 1),
					Vectors.DownOne,
					Vectors.RightOne
				),
				TraversalDirection.RightToLeft =>
				(
					new Location(rectangle.Right, rectangle.Top),
					new Location(rectangle.Left - 1, rectangle.Top),
					new Location(rectangle.Right, rectangle.Bottom + 1),
					Vectors.DownOne,
					Vectors.LeftOne
				),
				TraversalDirection.UpToDown =>
				(
					rectangle.TopLeft,
					new Location(rectangle.Left, rectangle.Bottom + 1),
					new Location(rectangle.Right + 1, rectangle.Top),
					Vectors.RightOne,
					Vectors.DownOne
				),
				TraversalDirection.DownToUp =>
				(
					new Location(rectangle.Left, rectangle.Bottom),
					new Location(rectangle.Left, rectangle.Top - 1),
					new Location(rectangle.Right + 1, rectangle.Bottom),
					Vectors.RightOne,
					Vectors.UpOne
				),
				_ => throw new InvalidOperationException("Not all traversal directions handled")
			};

			for (
				;
					lineBeginning != terminationPoint;
					lineBeginning = lineBeginning + lineIncrement,
					lineEnd = lineEnd + lineIncrement
				)
			{
				handleLineStart.Invoke();
				for (
					var l = lineBeginning;
					l != lineEnd;
					l = l + cellIncrement
				)
				{
					handleCell.Invoke(l);
				}
			}
		}

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
			new Rectangle
			(
				r.TopLeft + v,
				r.Width,
				r.Height
			);
	}
}
