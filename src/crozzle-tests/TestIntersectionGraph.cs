using crozzle;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace crozzle_tests
{
	using crozzle_graph;

	class TestIntersectionGraph
	{
		[Test]
		public async Task FromWorkspaceAndBackAgain()
		{
			var solution = await TestDataReader.ReadWorkspace(
				"crozzle_tests.TestData.Michael-Words.txt",
				"crozzle_tests.TestData.Michael-Solution.txt"
			);
			var wordDatabase = solution.WordDatabase;
			var graphEnvironment = GraphEnvironment.Generate(wordDatabase);
			var intersectionSolution = graphEnvironment.Convert(solution);
			var convertedBack = graphEnvironment.Convert(intersectionSolution);
			Assert.AreEqual(solution, convertedBack);
		}
	}
}
