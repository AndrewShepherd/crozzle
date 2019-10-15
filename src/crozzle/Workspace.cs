namespace crozzle
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
		public int Score { get; set; } = 0;
		public Board Board;
		public ImmutableList<string> IncludedWords { get; set; }
		public ImmutableList<Intersection> Intersections { get; set; }


		public ImmutableList<Slot> Slots = ImmutableList<Slot>.Empty;
		public WordDatabase WordDatabase = WordDatabase.Empty;

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
			if(this.IsValid != w.IsValid)
			{
				return false;
			}
			if(!(this.WordDatabase.Equals(w.WordDatabase)))
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

		private int GenerateHashCode() =>
			Score.GetHashCode()
				^ Board.GetHashCode().RotateLeft(1)
				^ HashUtils.GenerateHash(Slots);

		private readonly Lazy<int> _lazyHashCode;
		public override int GetHashCode() => _lazyHashCode.Value;


		public static Workspace Generate(IEnumerable<string> words)
		{
			var workspace = new Workspace()
			{
				WordDatabase = WordDatabase.Generate(words),
				IncludedWords = ImmutableList<string>.Empty,
				Intersections = ImmutableList<Intersection>.Empty,
				Board = new Board
				{
					Rectangle = new Rectangle(new Location(0, 0), 0, 0)
				},
			};
			return workspace;
		}


		public bool IsValid { get; internal set; }

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
							0
							+ this.Slots.Select(c => Scoring.Score(c.Letter)).Sum()
								+ (this.Intersections.Count())
								/*- (this.IncludedWords.Select(w => w.Length).Sum())*/,
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
				IsValid = this.IsValid,
				WordDatabase = this.WordDatabase,
				Board = this.Board.Move(diff),
				IncludedWords = this.IncludedWords,
				Intersections = this.Intersections,
				Score = this.Score,
				Slots = ImmutableList<Slot>
					.Empty
					.AddRange(this.Slots.Select(slot => slot.Move(diff))),
				_potentialScore = this._potentialScore
			};

		}
			
	}
}
