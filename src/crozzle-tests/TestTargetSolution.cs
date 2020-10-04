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

		[Test]
		public async Task MatchTarget()
		{
			string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
			var wordList = await ReadStringList("crozzle_tests.TestData.Michael-Words.txt");
			Assert.That(wordList, Has.Count.GreaterThan(0));
			var workspace = Workspace.Generate(wordList);

			var solutionGrid = await ReadStringList("crozzle_tests.TestData.Michael-Solution.txt");
			var solutionWordPlacements = ReadWordPlacements(solutionGrid).ToList();
			Assert.That(solutionWordPlacements, Has.Count.GreaterThan(0));

			foreach(var wordPlacement in solutionWordPlacements)
			{
				workspace = workspace.PlaceWord(
					wordPlacement.Direction,
					wordPlacement.Word,
					wordPlacement.Location.X,
					wordPlacement.Location.Y
				);
			}
			Assert.That(workspace.IsValid);
		}
	}
}
