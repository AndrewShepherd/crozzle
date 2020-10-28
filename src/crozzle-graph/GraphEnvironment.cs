using crozzle;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace crozzle_graph
{
	public class GraphEnvironment
	{
		public static GraphEnvironment Generate(WordDatabase wordDatabase)
		{
			var intersections = IntersectionBuilder.GetIntersections(wordDatabase);
			return new GraphEnvironment
			{
				Intersections = new HashSet<Intersection>(intersections),
				WordDatabase = wordDatabase,
			};
		}

		private HashSet<Intersection> Intersections { get; set; } = new HashSet<Intersection>();

		private WordDatabase WordDatabase { get; set; } = WordDatabase.Empty;


		private static IEnumerable<Intersection> GetAllIntersections(Workspace w)
		{
			var cells = new WordAndIndex[w.Board.Rectangle.Area];
			foreach (var wordPlacement in w.Board.WordPlacements)
			{
				for (
					(int i, Location l) = (0, wordPlacement.Location);
					i < wordPlacement.Word.Length;
					++i,
					l = wordPlacement.Direction == Direction.Across
					? new Location(l.X + 1, l.Y)
					: new Location(l.X, l.Y + 1)
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
							this.Intersections.Add(i);
							return i;
						}
					}
				).ToImmutableHashSet();

			return new IntersectionSolution
			{ 
				Workspace = w,
				Intersections = intersections
			};
		}


		class AdjacencyNode
		{
			public string Word;
			public bool Visited = false;
			public Direction? Direction;
			public Location Location;
		}

		public Workspace Convert(IntersectionSolution s)
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
			var wordIndexLookup = new Dictionary<string, int>();
			for(int i = 0; i < adjacencyNodes.Length; ++i)
			{
				wordIndexLookup[adjacencyNodes[i].Word] = i;
			}
			var adajcencyMatrix = new Intersection[adjacencyNodes.Length, adjacencyNodes.Length];
			foreach(var intersection in s.Intersections)
			{
				var i1 = wordIndexLookup[intersection.First.Word];
				var i2 = wordIndexLookup[intersection.Second.Word];
				adajcencyMatrix[i1, i2] = intersection;
				adajcencyMatrix[i2, i1] = intersection;
			}

			adjacencyNodes[0].Direction = Direction.Across;
			adjacencyNodes[0].Location = new Location(0, 0);
			AdjacencyNode nextUnvisited = null;
			while ( (
						nextUnvisited = adjacencyNodes.FirstOrDefault(
							n => n.Direction.HasValue && !n.Visited
						)
					) != null
				)
			{
				int i = wordIndexLookup[nextUnvisited.Word];
				for(int j = 0; j < adjacencyNodes.Length; ++j)
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
					if(otherNode.Direction.HasValue)
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
			foreach(var wp in wordPlacements)
			{
				rectangle = Rectangle.Union(
					rectangle,
					wp.GetRectangle()
				);
			}
			if(WorkspaceExtensions.RectangleIsTooBig(rectangle))
			{
				wordPlacements = wordPlacements
					.Select(wp => wp.Transpose())
					.ToArray();
			}
			var workspace = Workspace.Generate(this.WordDatabase.AllWords);
			foreach (var n in adjacencyNodes)
			{
				workspace = workspace.PlaceWord(
					n.Direction.Value,
					n.Word,
					n.Location.X,
					n.Location.Y
				);
			}

			return workspace.Normalise();
		}
	}
}
