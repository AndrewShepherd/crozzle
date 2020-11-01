using crozzle;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace crozzle_graph
{
	public class IntersectionRelationships
	{
		public ImmutableHashSet<Intersection> Enabled
		{
			get;
			internal set;
		} = ImmutableHashSet<Intersection>.Empty;
		public ImmutableHashSet<Intersection> Excluded
		{
			get;
			internal set;
		} = ImmutableHashSet<Intersection>.Empty;

		public void Exclude(Intersection intersection)
		{
			this.Excluded = this.Excluded.Add(intersection);
			this.Enabled = this.Enabled.Remove(intersection);
		}

		public void Enable(Intersection intersection)
		{
			this.Enabled = this.Enabled.Add(intersection);
		}
	}

	public class GraphEnvironment
	{

		public HashSet<Intersection> Intersections
		{ 
			get;
			private set;
		} = new HashSet<Intersection>();

		public Dictionary<Intersection, IntersectionRelationships> Relationships = new Dictionary<Intersection, IntersectionRelationships>();

		private WordDatabase WordDatabase { get; set; } = WordDatabase.Empty;


		public static GraphEnvironment Generate(WordDatabase wordDatabase)
		{
			var intersections = IntersectionBuilder.GetIntersections(wordDatabase);

			var intersectionRelationships = new Dictionary<Intersection, IntersectionRelationships>();
			foreach (var intersection in intersections)
			{
				intersectionRelationships.Add(
					intersection,
					new IntersectionRelationships()
				);
			}
			var array = intersections.ToArray();
			for (int i = 1; i < array.Length; ++i)
			{
				for (int j = 0; j < i; ++j)
				{
					var i1 = array[i];
					var i2 = array[j];
					var comparisonResult = Compare(
						i1,
						i2
					);
					if(comparisonResult.Relationship == IntersectionRelationship.Excludes)
					{
						intersectionRelationships[i1].Exclude(i2);
						intersectionRelationships[i2].Exclude(i1);
					}
					else if (comparisonResult.Relationship == IntersectionRelationship.Allows)
					{
						var theseWords = new[]
						{ 
							i1.First.Word,
							i1.Second.Word,
							i2.First.Word,
							i2.Second.Word 
						}.Distinct();
						var allCanMatch = comparisonResult
							.WordFragmentsCreated
							.Select(
								f =>
									wordDatabase
										.ListAvailableMatchingWords(f)
										.Where(w => !theseWords.Contains(w.Word))
										.Any()
							).All(_ => _);
						if(allCanMatch)
						{
							intersectionRelationships[i1].Enable(i2);
							intersectionRelationships[i2].Enable(i1);
						}
						else
						{
							intersectionRelationships[i1].Exclude(i2);
							intersectionRelationships[i2].Exclude(i1);
						}
					}
				}
			}
			return new GraphEnvironment
			{
				Intersections = new HashSet<Intersection>(intersections),
				Relationships = intersectionRelationships,
				WordDatabase = wordDatabase,
			};
		}


		private static IEnumerable<Intersection> GetAllIntersections(Workspace w)
		{
			var cells = new WordAndIndex[w.Board.Rectangle.Area];
			foreach (var wordPlacement in w.Board.WordPlacements)
			{
				for (
					(int i, Location l) = (0, wordPlacement.Location);
					i < wordPlacement.Word.Length;
					++i,
					l = l.Offset(wordPlacement.Direction, 1)
				)
				{
					int index = w.Board.Rectangle.IndexOf(l);
					if (cells[index] == null)
					{
						cells[index] = new WordAndIndex(wordPlacement.Word, i);
					}
					else
					{
						yield return new Intersection(
							cells[index],
							new WordAndIndex
							(
								wordPlacement.Word,
								i
							)
						);
					}
				}
			}
		}

		public enum IntersectionRelationship
		{
			Orthoganal,
			Excludes,
			Allows
		};

		public class IntersectionComparisonResult
		{
			public IntersectionRelationship Relationship
			{
				get;
				set;
			} = IntersectionRelationship.Orthoganal;

			public IEnumerable<string> WordFragmentsCreated
			{
				get;
				set;
			} = Enumerable.Empty<string>();

			public static IntersectionComparisonResult Excludes =
				new IntersectionComparisonResult
				{
					Relationship = IntersectionRelationship.Excludes
				};

			public static IntersectionComparisonResult Orthogonal =
				new IntersectionComparisonResult
				{
					Relationship = IntersectionRelationship.Orthoganal
				};

			public static IntersectionComparisonResult Allows(
				IEnumerable<string> fragments = null
			) =>
				new IntersectionComparisonResult
				{
					Relationship = IntersectionRelationship.Allows,
					WordFragmentsCreated = fragments ?? Enumerable.Empty<string>()
				};
		}

		public static IntersectionComparisonResult Compare(Intersection i1, Intersection i2)
		{
			bool atLeastOneWordMatches = (i1.First.Word == i2.First.Word)
				|| (i1.First.Word == i2.Second.Word)
				|| (i1.Second.Word == i2.First.Word)
				|| (i1.Second.Word == i2.Second.Word);
			if (!atLeastOneWordMatches)
			{
				return IntersectionComparisonResult.Orthogonal;
			}
			if (
				(i1.First.Word, i1.Second.Word) == (i2.First.Word, i2.Second.Word)
			)
			{
				return IntersectionComparisonResult.Excludes;
			}
			var i1Placements = new[] { i1.First, i1.Second };
			var i2Placements = new[] { i2.First, i2.Second };
			foreach (var p1 in i1Placements)
			{
				foreach (var p2 in i2Placements)
				{
					if (p1.Equals(p2))
					{
						return IntersectionComparisonResult.Excludes;
					}
				}
			}
			var joinedByWord = i1Placements
				.Join(
					i2Placements,
					p => p.Word,
					p => p.Word,
					(p1, p2) => Tuple.Create(p1, p2)
				).ToArray();
			if(joinedByWord.Length == 2)
			{
				return IntersectionComparisonResult.Excludes;
			}
			if(joinedByWord.Length == 0)
			{
				return IntersectionComparisonResult.Orthogonal;
			}
			List<string> fragments = new List<string>();
			var matchingPlacements = joinedByWord[0];
			if(
				Math.Abs(
					matchingPlacements.Item1.Index - matchingPlacements.Item2.Index
				) > 1
			)
			{
				return IntersectionComparisonResult.Allows();
			}
			string sharedWord = matchingPlacements.Item1.Word;
			var other1 = i1.First == matchingPlacements.Item1 ? i1.Second : i1.First;
			var other2 = i2.First == matchingPlacements.Item2 ? i2.Second : i2.First;
			(other1, other2) = matchingPlacements.Item1.Index < matchingPlacements.Item2.Index
				? (other1, other2)
				: (other2, other1);
			int other2Offset = other2.Index - other1.Index;

			int index1 = (other2Offset > 0 ? 0 : 0 - other2Offset);
			int index2 = index1 + other2Offset;
			for (
				;
				index1 < other1.Word.Length && index2 < other2.Word.Length;
				++index1,
				++index2
			)
			{
				if(index1 != other1.Index)
				{
					try
					{
						string fragment = new string(
							new[]
							{
							other1.Word[index1],
							other2.Word[index2]
							}
						);
						fragments.Add(fragment);
					}
					catch(IndexOutOfRangeException ex)
					{
					}
				}
			}
			return IntersectionComparisonResult.Allows(fragments);
		}

		public IEnumerable<Intersection> GetAvailableIntersections(
			IntersectionSolution intersectionSolution
		)
		{
			var relationships = intersectionSolution
				.Intersections
				.Select(i => this.Relationships[i])
				.ToList();

			var exclusions = relationships
				.Select(r => r.Excluded)
				.Aggregate(
					intersectionSolution.ExcludedIntersections,
					(l, r) => l.Union(r)
				);
			var inclusions = relationships
				.Select(r => r.Enabled.Except(exclusions))
				.Aggregate(
					ImmutableHashSet<Intersection>.Empty,
					(l, r) => l.Union(r)
				);
			return inclusions;
					
		}

		public IntersectionSolution Convert(Workspace w)
		{
			var intersections = GetAllIntersections(w)
				.Select(
					i =>
					{
						if (this.Intersections.TryGetValue(i, out Intersection v))
						{
							return v;
						}
						else
						{
							if(i.First.Word[i.First.Index] != i.Second.Word[i.Second.Index])
							{
								throw new InvalidOperationException("Invalid index created");
							}
							this.Intersections.Add(i);
							return i;
						}
					}
				).ToImmutableHashSet();
			return new IntersectionSolution
			{ 
				Intersections = intersections,
			};
		}


		class AdjacencyNode
		{
			public string Word;
			public bool Visited = false;
			public Direction? Direction;
			public Location Location;
		}

		private static IEnumerable<WordPlacement> ConvertToWordPlacements(IntersectionSolution s)
		{
			var adjacencyNodes = s
				.Intersections
				.SelectMany(i => new[] { i.First.Word, i.Second.Word })
				.Distinct()
				.Select(
					word =>
						new AdjacencyNode
						{
							Word = word,
							Visited = false
						}
				).ToArray();
			if(adjacencyNodes.Length == 0)
			{
				return Enumerable.Empty<WordPlacement>();
			}
			var wordIndexLookup = new Dictionary<string, int>();
			for (int i = 0; i < adjacencyNodes.Length; ++i)
			{
				wordIndexLookup[adjacencyNodes[i].Word] = i;
			}
			var adajcencyMatrix = new Intersection[adjacencyNodes.Length, adjacencyNodes.Length];
			foreach (var intersection in s.Intersections)
			{
				var i1 = wordIndexLookup[intersection.First.Word];
				var i2 = wordIndexLookup[intersection.Second.Word];
				adajcencyMatrix[i1, i2] = intersection;
				adajcencyMatrix[i2, i1] = intersection;
			}
			

			adjacencyNodes[0].Direction = Direction.Across;
			adjacencyNodes[0].Location = new Location(0, 0);
			AdjacencyNode nextUnvisited = null;
			while ((
						nextUnvisited = adjacencyNodes.FirstOrDefault(
							n => n.Direction.HasValue && !n.Visited
						)
					) != null
				)
			{
				int i = wordIndexLookup[nextUnvisited.Word];
				for (int j = 0; j < adjacencyNodes.Length; ++j)
				{
					var intersection = adajcencyMatrix[i, j];
					if (intersection == null)
					{
						continue;
					}
					(
						var thisWordAndIndex,
						var otherWordAndIndex
					) = (
						intersection.First.Word == nextUnvisited.Word
						? (intersection.First, intersection.Second)
						: (intersection.Second, intersection.First)
					);
					var otherNode = adjacencyNodes[wordIndexLookup[otherWordAndIndex.Word]];
					if (otherNode.Direction.HasValue)
					{
						continue;
					}
					otherNode.Direction = nextUnvisited.Direction == Direction.Across
						? Direction.Down
						: Direction.Across;
					var intersectionLocation = nextUnvisited.Location.Offset(
						nextUnvisited.Direction.Value,
						thisWordAndIndex.Index
					);
					otherNode.Location = intersectionLocation.Offset(
						otherNode.Direction.Value,
						0 - otherWordAndIndex.Index
					);
				}
				nextUnvisited.Visited = true;
			}
			var wordPlacements = adjacencyNodes
				.Select(
					n =>
						new WordPlacement
						(
							n.Direction.Value,
							n.Location,
							n.Word
						)
				).ToArray();
			var rectangle = Rectangle.Empty;
			foreach (var wp in wordPlacements)
			{
				rectangle = Rectangle.Union(
					rectangle,
					wp.GetRectangle()
				);
			}
			if (WorkspaceExtensions.RectangleIsTooBig(rectangle))
			{
				wordPlacements = wordPlacements
					.Select(wp => wp.Transpose())
					.ToArray();
			}
			return wordPlacements;
		}

		public Workspace Convert(IntersectionSolution s)
		{
			var workspace = Workspace.Generate(this.WordDatabase);
			foreach(var n in ConvertToWordPlacements(s))
			{
				workspace = workspace.PlaceWord(
					n.Direction,
					n.Word,
					n.Location.X,
					n.Location.Y
				);
			};
			return workspace.Normalise();
		}
	}
}
