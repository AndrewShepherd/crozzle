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

		static async Task<List<string>> ReadStringList(string manifestResourceStreamName)
		{
			List<string> rv = new List<string>();
			using (var wordsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestResourceStreamName))
			{
				if(wordsStream == null)
				{
					throw new InvalidOperationException($"Could not load the manifest resource {manifestResourceStreamName}");
				}
				using (var streamReader = new StreamReader(wordsStream))
				{
					string? s = await streamReader.ReadLineAsync();
					while (s != null)
					{
						rv.Add(s);
						s = await streamReader.ReadLineAsync();
					}
				}
			}
			return rv;
		}

		
		static IEnumerable<WordPlacement> ReadWordPlacements(List<string> solutionGrid)
		{
			for(int i = 0; i < solutionGrid.Count; ++i)
			{
				string partialWord = string.Empty;
				for(int j = 0; j < solutionGrid[i].Length; ++j)
				{
					var c = solutionGrid[i][j];
					if(char.IsLetter(c))
					{
						partialWord = $"{partialWord}{char.ToUpper(c)}";
					}
					else
					{
						if(partialWord.Length > 1)
						{
							yield return new WordPlacement(
								Direction.Across,
								new Location(j - partialWord.Length, i),
								partialWord
							);
						}
						partialWord = string.Empty;
					}
				}
				if (partialWord.Length > 1)
				{
					yield return new WordPlacement(
						Direction.Across,
						new Location(solutionGrid[i].Length - partialWord.Length, i),
						partialWord
					);
				}
			}
			for(int i = 0; i < solutionGrid[0].Length; ++i)
			{
				string partialWord = string.Empty;
				for(int j = 0; j < solutionGrid.Count; ++j)
				{
					var c = solutionGrid[j][i];
					if(char.IsLetter(c))
					{
						partialWord = $"{partialWord}{char.ToUpper(c)}";
					}
					else
					{
						if(partialWord.Length > 1)
						{
							yield return new WordPlacement(
								Direction.Down,
								new Location(i, j-partialWord.Length),
								partialWord
							);
						}
						partialWord = string.Empty;
					}
				}
				if (partialWord.Length > 1)
				{
					yield return new WordPlacement(
						Direction.Across,
						new Location(solutionGrid.Count - partialWord.Length, i),
						partialWord
					);
				}
			}
		}

		bool IsWorkspaceSubsetOf(Workspace workspace, Workspace target)
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
			var wordList = await ReadStringList("crozzle_tests.TestData.Michael-Words.txt");
			Assert.That(wordList, Has.Count.GreaterThan(0));


			var solutionGrid = await ReadStringList("crozzle_tests.TestData.Michael-Solution.txt");
			var solutionWordPlacements = ReadWordPlacements(solutionGrid).ToList();
			Assert.That(solutionWordPlacements, Has.Count.GreaterThan(0));

			var targetWorkspace = Workspace.Generate(wordList);
			foreach (var wordPlacement in solutionWordPlacements)
			{
				targetWorkspace = targetWorkspace.PlaceWord(
					wordPlacement.Direction,
					wordPlacement.Word,
					wordPlacement.Location.X,
					wordPlacement.Location.Y
				);
			}
			targetWorkspace = targetWorkspace.Normalise();
			Assert.That(targetWorkspace.IsValid);


			var sourceWorkspace = Workspace.Generate(wordList);
			IEnumerable<Workspace> steps = solutionWordPlacements
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
