namespace crozzle
{
	public class CandidateWord
	{
		public string Word { get; set; }
		public int MatchIndex { get; set; }

		public override bool Equals(object obj) =>
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
			this CandidateWord candidateWord,
			Location location,
			Direction direction
		)
		{
			Location l = direction == Direction.Across
				? new Location(location.X - candidateWord.MatchIndex, location.Y)
				: new Location(location.X, location.Y - candidateWord.MatchIndex);
			return new WordPlacement
			(
				direction,
				l,
				candidateWord.Word
			);
		}
	}
}
