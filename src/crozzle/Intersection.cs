namespace crozzle
{
	using System;
	using System.Diagnostics.CodeAnalysis;

	public class Intersection : IComparable<Intersection>
	{
		public readonly WordAndIndex First;
		public readonly WordAndIndex Second;

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

		public override bool Equals(object? obj) =>
			ReferenceEquals(this, obj)
			|| (
				obj is Intersection other
				&& this.First.Equals(other.First)
				&& this.Second.Equals(other.Second)
			);

		public override int GetHashCode() =>
			this.First.GetHashCode() ^ this.Second.GetHashCode();
	}

}
