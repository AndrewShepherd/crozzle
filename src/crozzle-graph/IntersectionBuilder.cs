namespace crozzle_graph
{
	using System.Collections.Generic;
	using System.Linq;
	using crozzle;

	public static class IntersectionBuilder
	{
		public static IEnumerable<Intersection> GetIntersections(WordDatabase wordDatabase)
		{
			wordDatabase = wordDatabase.ResetWordAvailability();
			for (var letter = 'A'; letter <= 'Z'; ++letter)
			{
				var candidateWords = wordDatabase.ListAvailableMatchingWords($"{letter}")
					.ToArray();
				for (int i = 0; i < candidateWords.Length; ++i)
				{
					for (int j = i + 1; j < candidateWords.Length; ++j)
					{
						(var ci, var cj) = (candidateWords[i], candidateWords[j]);
						if (ci.Word != cj.Word)
						{
							yield return new Intersection(ci, cj);
						}
					}
				}
			}
		}
	}
}
