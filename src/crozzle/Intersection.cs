namespace crozzle
{
	using System;
	using System.Diagnostics.CodeAnalysis;

	public record Intersection : IComparable<Intersection>
	{
		public WordAndIndex First { get; init; }
		public WordAndIndex Second { get; init; }

		public Intersection(WordAndIndex first, WordAndIndex second) =>
			(this.First, this.Second) = first.CompareTo(second) switch
			{
				int n when n < 0 => (first, second),
				_ => (second, first)
			};

		public int CompareTo([AllowNull] Intersection other) =>
			other == null
			? -1
			: this.First.CompareTo(other.First) switch
			{
				0 => this.Second.CompareTo(other.Second),
				int n => n
			};
	}

}
