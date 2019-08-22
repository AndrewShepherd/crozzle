namespace solve_crozzle
{
	public static class HashUtils
	{
		public static int RotateLeft(this int value, int count)
		{
			uint val = (uint)value;
			return (int)((val << count) | (val >> (32 - count)));
		}

		public static int RotateRight(this int value, int count)
		{
			uint val = (uint)value;
			return (int)((value >> count) | (value << (32 - count)));
		}
	}
}
