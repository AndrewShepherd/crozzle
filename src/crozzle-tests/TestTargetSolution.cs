using NUnit.Framework;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using crozzle;
using System.Linq;
using System.Collections.Immutable;

namespace crozzle_tests
{
	public class TestTargetSolution
	{
		static bool IsWorkspaceSubsetOf(Workspace workspace, Workspace target)
		{
			var workspaceWordPlacements = workspace.Board.WordPlacements;
			var targetWordPlacements = target.Board.WordPlacements;
			if(!workspaceWordPlacements.Any())
			{
				return true;
			}
			// Get the offset
			var first = workspaceWordPlacements.First();
			var matchingFirst = targetWordPlacements.Where(twp => twp.Word == first.Word).FirstOrDefault();
			if(matchingFirst == null)
			{
				return false;
			}
			if(matchingFirst.Direction != first.Direction)
			{
				return false;
			}
			Vector offset = matchingFirst.Location - first.Location;
			foreach(var wp in workspaceWordPlacements.Skip(1))
			{
				var matching = targetWordPlacements.Where(twp => twp.Word == wp.Word).FirstOrDefault();
				if(matching == null)
				{
					return false;
				}
				if (!(wp.Move(offset).Equals(matching)))
				{
					return false;
				}
			}
			return true;
		}

		[Test]
		public async Task MatchTarget()
		{
			string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			var wordList = await TestDataReader.ReadStringList("crozzle_tests.TestData.Michael-Words.txt");
			Assert.That(wordList, Has.Count.GreaterThan(0));
			var targetWorkspace = await TestDataReader.ReadWorkspace(
				"crozzle_tests.TestData.Michael-Words.txt",
				"crozzle_tests.TestData.Michael-Solution.txt"
			);
			

			Assert.That(targetWorkspace.IsValid);


			var sourceWorkspace = Workspace.Generate(targetWorkspace.WordDatabase.AllWords);
			IEnumerable<Workspace> steps = targetWorkspace.Board.WordPlacements // solutionWordPlacements
				.Select(
					wp =>
						sourceWorkspace
							.PlaceWord(
								wp.Direction,
								wp.Word,
								0,
								0
							)
				).ToArray();

			INextStepGenerator nextStepGenerator = new SlotFillingNextStepGenerator(3);
			bool found = false;
			Workspace perfectMatch = null;

			WorkspacePriorityQueue queue = new WorkspacePriorityQueue(100000);
			while (!found)
			{
				var candidates = steps.Select(
					s =>
						nextStepGenerator
							.GenerateNextSteps(s)
				).SelectMany(_ => _)
				.ToList();

				int countBefore = candidates.Count();
				candidates = candidates.Distinct().ToList();
				int countBeforeQueuing = candidates.Count();
				queue.Swap(
					candidates.Select(c => new WorkspaceNode { Ancestry = ImmutableList.Create<int>(), Workspace = c}),
					0
				);

				candidates = new List<Workspace>();
				while(queue.Count > 0)
				{
					candidates.AddRange(
						queue.Swap(
							Enumerable.Empty<WorkspaceNode>(),
							1
						).Select(wn => wn.Workspace)
					);
				}
				int countAfterQueuing = candidates.Count();
				//Assert.That(countBeforeQueuing, Is.EqualTo(countAfterQueuing));
				//Assert.That(countBefore, Is.EqualTo(countAfter));
				steps = candidates.Where(ns => IsWorkspaceSubsetOf(ns, targetWorkspace))
					.ToList();
				perfectMatch = steps.Where(s => s.Board.WordPlacements.Count == targetWorkspace.Board.WordPlacements.Count)
					.FirstOrDefault();
				found = perfectMatch != null;
			}
			Assert.That(found, Is.True);
		}
	}
}
