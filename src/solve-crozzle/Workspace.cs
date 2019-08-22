namespace solve_crozzle
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	public class Intersection
	{
		public string Word;
		public int Index;
	}

	public class Workspace
	{
		public int Score = 0;
		public Board Board;
		public ImmutableHashSet<string> AvailableWords;
		public ImmutableList<string> IncludedWords;
		public ImmutableList<Intersection> Intersections;


		public ImmutableList<Slot> Slots = ImmutableList<Slot>.Empty;
		public ImmutableList<PartialWord> PartialWords = ImmutableList<PartialWord>.Empty;

		public Workspace()
		{
			_lazyHashCode = new Lazy<int>(() => this.GenerateHashCode());
		}

		public override bool Equals(object obj)
		{
			if(object.ReferenceEquals(this, obj))
			{
				return true;
			}
			if(!(obj is Workspace w))
			{
				return false;
			}
			if(this.Score != w.Score)
			{
				return false;
			}
			if(!(this.AvailableWords.SetEquals(w.AvailableWords)))
			{
				return false;
			}
			if(!Enumerable.SequenceEqual(this.PartialWords, w.PartialWords))
			{
				return false;
			}
			if(!Enumerable.SequenceEqual(
				this.Slots.OrderBy(_ => _), 
				w.Slots.OrderBy(_ => _)
			))
			{
				return false;
			}
			if (!(this.Board.Equals(w.Board)))
			{
				return false;
			}
			return true;
		}

		public static int GenerateHash<T>(IEnumerable<T> t) =>
			t.Aggregate(
				0,
				(h, item) => h ^ item.GetHashCode()
			);

		private int GenerateHashCode() =>
			Score.GetHashCode()
				^ Board.GetHashCode().RotateLeft(1)
				^ GenerateHash(Slots);

		private readonly Lazy<int> _lazyHashCode;
		public override int GetHashCode() => _lazyHashCode.Value;


		internal static Workspace Generate(IEnumerable<string> words)
		{
			var workspace = new Workspace()
			{
				AvailableWords = ImmutableHashSet<string>.Empty,
				WordLookup = new Dictionary<string, List<String>>(),
				IncludedWords = ImmutableList<string>.Empty,
				Intersections = ImmutableList<Intersection>.Empty,
				Board = new Board
				{
					Rectangle = new Rectangle(new Location(0, 0), 0, 0)
				}
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
			}
			foreach(var key in workspace.WordLookup.Keys.ToList())
			{
				workspace.WordLookup[key] = workspace.WordLookup[key].OrderByDescending(w => Scoring.Score(w)).ToList();
			}
			return workspace;
		}

		public Dictionary<string, List<String>> WordLookup = null;

		public bool IsValid => PartialWords.IsEmpty;

		public string BoardRepresentation => Board.ToString();

		public override string ToString()
		{
			return $"WordCount: {this.IncludedWords.Count} PotentialScore: {this.PotentialScore}";

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
					_potentialScore = this.Score
						+ Math.Min(
							this.Slots.Select(c => Scoring.Score(c.Letter)).Sum()
								+ (this.Intersections.Count())
								- (this.IncludedWords.Select(w => w.Length).Sum()),
							0);
				}
				return _potentialScore.Value;
			}
		}

		private static Location Zero = new Location(0, 0);

		public Workspace Normalise()
		{
			var topLeft = this.Board.Rectangle.TopLeft;
			if(topLeft.Equals(Zero))
			{
				return this;
			}
			var diff = Zero - topLeft;
			return new Workspace
			{
				AvailableWords = this.AvailableWords,
				Board = this.Board.Move(diff),
				IncludedWords = this.IncludedWords,
				Intersections = this.Intersections,
				PartialWords = ImmutableList<PartialWord>
					.Empty
					.AddRange(this.PartialWords.Select(pw => pw.Move(diff))),
				Score = this.Score,
				Slots = ImmutableList<Slot>
					.Empty
					.AddRange(this.Slots.Select(slot => slot.Move(diff))),
				WordLookup = this.WordLookup,
				_potentialScore = this._potentialScore
			};

		}
			
	}
}
