namespace crozzle
{
	using System.Collections.Generic;
	using System.Linq;

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

		public static int GenerateHash<T>(IEnumerable<T> t) =>
			t.Aggregate(
				0,
				(h, item) => h ^ item.GetHashCode()
			);
	}
}
