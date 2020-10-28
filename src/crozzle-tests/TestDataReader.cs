using crozzle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace crozzle_tests
{
	static class TestDataReader
	{
		internal static async Task<List<string>> ReadStringList(string manifestResourceStreamName)
		{
			List<string> rv = new List<string>();
			using (var wordsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestResourceStreamName))
			{
				if (wordsStream == null)
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

		internal static async Task<Workspace> ReadWorkspace(string availableWordsResourceId, string solutionResourceId)
		{
			var wordList = await ReadStringList(availableWordsResourceId);
			var solutionGrid = await ReadStringList(
				solutionResourceId
			);
			var solutionWordPlacements = TestDataReader.ReadWordPlacements(solutionGrid)
				.ToList();
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
			return targetWorkspace.Normalise();
		}

		internal static IEnumerable<WordPlacement> ReadWordPlacements(List<string> solutionGrid)
		{
			for (int i = 0; i < solutionGrid.Count; ++i)
			{
				string partialWord = string.Empty;
				for (int j = 0; j < solutionGrid[i].Length; ++j)
				{
					var c = solutionGrid[i][j];
					if (char.IsLetter(c))
					{
						partialWord = $"{partialWord}{char.ToUpper(c)}";
					}
					else
					{
						if (partialWord.Length > 1)
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
			for (int i = 0; i < solutionGrid[0].Length; ++i)
			{
				string partialWord = string.Empty;
				for (int j = 0; j < solutionGrid.Count; ++j)
				{
					var c = solutionGrid[j][i];
					if (char.IsLetter(c))
					{
						partialWord = $"{partialWord}{char.ToUpper(c)}";
					}
					else
					{
						if (partialWord.Length > 1)
						{
							yield return new WordPlacement(
								Direction.Down,
								new Location(i, j - partialWord.Length),
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

	}
}
