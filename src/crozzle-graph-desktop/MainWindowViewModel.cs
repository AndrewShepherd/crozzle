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
				foreach(var splitSolution in Split(s, graphEnvironment))
				{
					stack.Push(s);
				}
			}
			
			var intersections = IntersectionBuilder.GetIntersections(wordDatabase).ToArray();
			int l = intersections.Length;
		}

		public MainWindowViewModel()
		{
			var task = DoStuff();
		}
	}
}
