using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace crozzle_graph_desktop
{
    using crozzle;
    using System.IO;
	using System.Linq;
	public class MainWindowViewModel
	{
		public const string FilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\too-many-zeds.txt";

		private async Task<WordDatabase> GenerateWordDatabaseAsync()
		{
			using var stream = File.OpenRead(FilePath);
			var words = await WordStreamReader.Read(stream);
			return WordDatabase.Generate(words);
		}

		class WordAndIndex
		{
			public readonly string Word;
			public readonly int Index;
			public WordAndIndex(string word, int index)
			{
				this.Word = word;
				this.Index = index;
			}
		}

		class Intersection
		{
			public readonly WordAndIndex First;
			public readonly WordAndIndex Second;
			public readonly Intersection Converse;

			public Intersection(WordAndIndex first, WordAndIndex second)
			{
				this.First = first;
				this.Second = second;
				this.Converse = new Intersection(second, first, this);
			}

			private Intersection(WordAndIndex first, WordAndIndex second, Intersection converse)
			{
				this.First = first;
				this.Second = second;
				this.Converse = converse;
			}
		}

		private static IEnumerable<Intersection> GetIntersections(WordDatabase wordDatabase)
		{
			for (var letter = 'A'; letter <= 'Z'; ++letter)
			{
				var candidateWords = wordDatabase.ListAvailableMatchingWords($"{letter}")
					.Select(
						cw =>
							new WordAndIndex(
								cw.Word,
								cw.MatchIndex
							)
					).ToArray();
				for (int i = 0; i < candidateWords.Length; ++i)
				{
					for (int j = i + 1; j < candidateWords.Length; ++j)
					{
						(var ci, var cj) = (candidateWords[i], candidateWords[j]);
						if (ci.Word != cj.Word)
						{
							yield return new Intersection(ci, cj);
						}
						else
						{
							int dummy = 3;
						}
					}
				}
			}
		}

		private async Task DoStuff()
		{
			WordDatabase wordDatabase = await GenerateWordDatabaseAsync();
			var intersections = GetIntersections(wordDatabase).ToArray();
			int l = intersections.Length;
			
		}

		public MainWindowViewModel()
		{
			var task = DoStuff();
		}
	}
}
