
namespace crozzle_graph
{
	using System;
	using System.Diagnostics.CodeAnalysis;

	public class WordAndIndex : IComparable<WordAndIndex>
	{
		public readonly string Word;
		public readonly int Index;
		public WordAndIndex(string word, int index)
		{
			this.Word = word;
			this.Index = index;
		}

		public int CompareTo([AllowNull] WordAndIndex other) =>
			other == null
			? 1
			: this.Word.CompareTo(other.Word) switch
			{
				0 => this.Index.CompareTo(other.Index),
				int n => n
			};

		public override int GetHashCode() =>
			this.Word.GetHashCode() ^ this.Index.GetHashCode();

		public override bool Equals(object obj) =>
			object.ReferenceEquals(this, obj)
			|| (
				obj is WordAndIndex other
				&& other.Word.Equals(this.Word)
				&& other.Index.Equals(this.Index)
			);
	}
}
