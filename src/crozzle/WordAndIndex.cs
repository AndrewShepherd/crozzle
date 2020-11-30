namespace crozzle
{
	using System;
	using System.Diagnostics.CodeAnalysis;

	public sealed record WordAndIndex : IComparable<WordAndIndex>
	{
		public string Word { get; init; }
		public int Index { get; init; }
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
	}

	public static class WordAndIndexExtensions
	{
		public static WordPlacement CreateWordPlacement(
			this WordAndIndex candidateWord,
			Location location,
			Direction direction
		)
		{
			Location l = direction == Direction.Across
				? new Location(location.X - candidateWord.Index, location.Y)
				: new Location(location.X, location.Y - candidateWord.Index);
			return new WordPlacement
			(
				direction,
				l,
				candidateWord.Word
			);
		}
	}
}
