using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace crozzle_graph_desktop
{
    using crozzle;
	using crozzle_graph;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	public class MainWindowViewModel
	{
		//public const string FilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\too-many-zeds.txt";
		public const string FilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\20190814.txt";


		private async Task<WordDatabase> GenerateWordDatabaseAsync()
		{
			using var stream = File.OpenRead(FilePath);
			var words = await WordStreamReader.Read(stream);
			return WordDatabase.Generate(words);
		}

		private IEnumerable<IntersectionSolution> Split(
			IntersectionSolution intersectionSolution,
			GraphEnvironment graphEnvironment
		)
		{
			IEnumerable<Intersection> availableIntersections = graphEnvironment.GetAvailableIntersections(intersectionSolution);

			if(!availableIntersections.Any())
			{
				yield break;
			}
			var workspace = graphEnvironment.Convert(intersectionSolution);
			if(workspace == null)
			{
				yield break;
			}

			var grid = workspace.GenerateGrid();
			Intersection nextIntersection = null;
			if(!workspace.IsValid)
			{
				var partialWord = grid.PartialWords.First();
				// TODO: Get the cell word and index
				var cell = grid.CellAt(partialWord.Rectangle.TopLeft);
				var item1 = cell.WordAndIndex;
				var matchingWords = graphEnvironment.WordDatabase.ListAvailableMatchingWords(partialWord.Value);
				foreach(var wordAndIndex in matchingWords)
				{
					var newIntersection = new Intersection(
						item1,
						wordAndIndex
					);
					if(availableIntersections.Contains(newIntersection))
					{
						nextIntersection = newIntersection;
						break;
					}
				}
				if (nextIntersection == null)
				{
					yield break;
				}
			}

			nextIntersection = nextIntersection ?? availableIntersections.First();

			WordPlacement newWordPlacement = null;
			// How do I convert this into a wordPlacement
			foreach(var wordPlacement in workspace.Board.WordPlacements)
			{

				if(wordPlacement.Word == nextIntersection.First.Word)
				{
					newWordPlacement = new WordPlacement
					(
						wordPlacement.Direction.Transpose(),
						wordPlacement
							.Location
							.Offset(
								wordPlacement.Direction,
								nextIntersection.First.Index
							).Offset(
								wordPlacement.Direction.Transpose(),
								0 - nextIntersection.Second.Index
							),
						nextIntersection.Second.Word
					);
					break;
				}
				else if (wordPlacement.Word == nextIntersection.Second.Word)
				{
					newWordPlacement = new WordPlacement
					(
						wordPlacement.Direction.Transpose(),
						wordPlacement
							.Location
							.Offset(
								wordPlacement.Direction,
								nextIntersection.Second.Index
							).Offset(
								wordPlacement.Direction.Transpose(),
								0 - nextIntersection.First.Index
							),
						nextIntersection.First.Word
					);
					break;
				}
			}
			if (newWordPlacement != null)
			{
				var newWorkspace = workspace.TryPlaceWord(grid, newWordPlacement);
				if (newWorkspace != null)
				{
					IntersectionSolution newSolution = null;
					try
					{
						newSolution = graphEnvironment.Convert(newWorkspace);
					}
					catch(Exception ex)
					{
						var testWorkspace = workspace.PlaceWord(newWordPlacement);
						yield break;
					}
					newSolution.ExcludedIntersections = intersectionSolution.ExcludedIntersections;
					// One final test
					var c = graphEnvironment.Convert(newSolution);
					if(c == null)
					{
						throw new InvalidOperationException();
					}
					var c2 = graphEnvironment.Convert(c);
					if(c2 == null)
					{
						throw new InvalidOperationException();
					}
					yield return newSolution;
					var newlyAddedIntersections = newSolution.Intersections
						.Except(intersectionSolution.Intersections);
					yield return new IntersectionSolution
					{
						Intersections = intersectionSolution.Intersections,
						ExcludedIntersections = intersectionSolution
							.ExcludedIntersections
							.Union(newlyAddedIntersections)
					};
				}
				else
				{
					yield return new IntersectionSolution
					{
						Intersections = intersectionSolution.Intersections,
						ExcludedIntersections = intersectionSolution
							.ExcludedIntersections
							.Add(nextIntersection)
					};
				}
			}
			yield break;
		}

		private async Task DoStuff()
		{
			WordDatabase wordDatabase = await GenerateWordDatabaseAsync();
			var graphEnvironment = GraphEnvironment.Generate(wordDatabase);
			var intersection = graphEnvironment.Intersections.First();

			var solution = new IntersectionSolution
			{
				Intersections = ImmutableHashSet<Intersection>
					.Empty
					.Add(intersection)
			};
			var stack = new Stack<IntersectionSolution>();
			stack.Push(solution);
			while(stack.TryPop(out IntersectionSolution s))
			{
				if(stack.Count == 0)
				{
					int dummy = 3;
				}
				foreach(var splitSolution in Split(s, graphEnvironment))
				{
					stack.Push(splitSolution);
				}
			}
		}

		public MainWindowViewModel()
		{
			var task = DoStuff();
		}
	}
}
