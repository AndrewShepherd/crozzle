using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public enum Direction {  Across, Down };

	public class Slot
	{
		public Direction Direction;
		public char Letter;
		public Location Location;
	}

	public class PartialWord
	{
		public Direction Direction;
		public string Value;
		public Location Location;
	}

	public class Intersection
	{
		public string Word;
		public int Index;
	}

	public class Workspace
	{
		public int Score = 0;
		public int Width = 0;
		public int Height = 0;
		public int XStart = 0;
		public int YStart = 0;
		public char[] Values = new char[0];
		public ImmutableHashSet<string> AvailableWords;
		public ImmutableList<string> IncludedWords;
		public ImmutableList<Intersection> Intersections;
		public int MaxWidth = 17;
		public int MaxHeight = 12;

		public ImmutableList<Slot> Slots = ImmutableList<Slot>.Empty;
		public ImmutableList<PartialWord> PartialWords = ImmutableList<PartialWord>.Empty;

		internal static Workspace Generate(IEnumerable<string> words)
		{
			ulong wordFlag = 0x1;
			var workspace = new Workspace()
			{
				AvailableWords = ImmutableHashSet<string>.Empty,
				WordLookup = new Dictionary<string, List<String>>(),
				IncludedWords = ImmutableList<string>.Empty,
				Intersections = ImmutableList<Intersection>.Empty
			};
			foreach (var word in words)
			{
				workspace.AvailableWords = workspace.AvailableWords.Add(word);
				for (int i = 0; i < word.Length; ++i)
				{
					for (int j = 1; j + i <= word.Length; ++j)
					{
						var substring = word.Substring(i, j);
						if (workspace.WordLookup.TryGetValue(substring, out var existingList))
						{
							if (!existingList.Contains(word))
								existingList.Add(word);
						}
						else
							workspace.WordLookup[word.Substring(i, j)] = new List<string> { word };
					}
				}
				wordFlag = wordFlag << 1;
			}
			return workspace;
		}


		public Dictionary<string, List<String>> WordLookup = null;

		public bool IsValid => PartialWords.IsEmpty;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < Values.Length; ++i)
			{
				sb.Append((Values[i] == (char)0) || (Values[i] == '*') ? '_' : Values[i]);
				if (i % Width == Width - 1)
				{
					sb.AppendLine();
				}
			}
			return sb.ToString();
		}

		public string GenerateScoreBreakdown()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"*** {IncludedWords.Count} Words ***");
			int c = 0;
			foreach (var word in IncludedWords)
			{
				sb.AppendLine($"{c++}\t{word}");
			}
			sb.AppendLine();
			sb.AppendLine($"*** {Intersections.Count} Intersections ***");
			c = 0;
			var cumulativeScore = 0;
			foreach(var intersection in Intersections)
			{
				var score = Scoring.Score(intersection.Word[intersection.Index]);
				cumulativeScore += score;
				sb.AppendLine($"{ c++}\t{intersection.Word[intersection.Index]}\t{score}\t{cumulativeScore}\t{intersection.Word}[{intersection.Index}]");
			}
			sb.AppendLine();
			sb.AppendLine($"{IncludedWords.Count}*10 + {cumulativeScore} = {IncludedWords.Count * 10 + cumulativeScore}");
			sb.AppendLine();
			return sb.ToString();
		}

		private int? _potentialScore;

		public int PotentialScore
		{
			get
			{
				if(!_potentialScore.HasValue)
				{
					_potentialScore = this.Score + this.Slots.Select(c => Scoring.Score(c.Letter)).Sum();
				}
				return _potentialScore.Value;
			}
		}
			
	}
}
