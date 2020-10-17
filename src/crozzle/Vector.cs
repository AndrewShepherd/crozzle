namespace crozzle
{
	public class Vector
	{
		public readonly int Dx;
		public readonly int Dy;

		public Vector(int dx, int dy)
		{
			Dx = dx;
			Dy = dy;
		}

		public override bool Equals(object? obj) =>
			(obj is Vector v)
			&& v.Dx == this.Dx
			&& v.Dy == this.Dy;

		public override int GetHashCode() =>
			(Dx << 16) ^ Dy;
	}

	public static class Vectors
	{
		public static Vector UpOne = new Vector(0, -1);
		public static Vector DownOne = new Vector(0, 1);
		public static Vector LeftOne = new Vector(-1, 0);
		public static Vector RightOne = new Vector(1, 0);
	}
}
