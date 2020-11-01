using System;

namespace crozzle
{
	[Obsolete("Should replace this with Word And Index")]
	public class CandidateWord
	{
		public string Word { get; set; } = string.Empty;
		public int MatchIndex { get; set; }

		public override bool Equals(object? obj) =>
			(obj is CandidateWord other)
			&& other.Word.Equals(Word)
			&& other.MatchIndex.Equals(MatchIndex);

		public override int GetHashCode() =>
			Word.GetHashCode() ^ MatchIndex;

		public override string ToString() =>
			$"{Word}[{MatchIndex}]";
	}

	internal static class CandidateWordExtensions
	{
		internal static WordPlacement CreateWordPlacement(
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
