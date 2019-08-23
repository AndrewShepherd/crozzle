using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace solve_crozzle
{
    public class WordDatabase
	{
		private ImmutableHashSet<string> AvailableWords = ImmutableHashSet<string>.Empty;
		private Dictionary<string, ImmutableHashSet<String>> WordLookup = new Dictionary<string, ImmutableHashSet<string>>();

		private WordDatabase()
		{
		}

		internal WordDatabase Remove(string word) =>
			new WordDatabase
			{
				AvailableWords = this.AvailableWords.Remove(word),
				WordLookup = this.WordLookup
			};

		public override int GetHashCode() =>
			HashUtils.GenerateHash(WordLookup) ^ WordLookup.GetHashCode();

		public override bool Equals(object obj) =>
			object.ReferenceEquals(this, obj)
			|| (
				(obj is WordDatabase wd)
				&& this.AvailableWords.SetEquals(wd.AvailableWords)
			);

		public static WordDatabase Generate(IEnumerable<string> words)
		{
			var wordDatabase = new WordDatabase();
			foreach (var word in words)
			{
				wordDatabase.AvailableWords = wordDatabase.AvailableWords.Add(word);
				for (int i = 0; i < word.Length; ++i)
				{
					for (int j = 1; j + i <= word.Length; ++j)
					{
						var substring = word.Substring(i, j);
						if (wordDatabase.WordLookup.TryGetValue(substring, out var existingList))
						{
							if (!existingList.Contains(word))
							{
								wordDatabase.WordLookup[substring] = existingList.Add(word);
							}

						}
						else
							wordDatabase.WordLookup[word.Substring(i, j)] = ImmutableHashSet<string>.Empty.Add(word);
					}
				}
			}
			return wordDatabase;
		}

		public IEnumerable<string> ListAvailableMatchingWords(string word)
		{
			if (this.WordLookup.TryGetValue(word, out var wordList))
				return wordList.Intersect(this.AvailableWords);
			else
				return Enumerable.Empty<string>();
		}

		public bool CanMatchWord(string word) =>
			ListAvailableMatchingWords(word).Any();
	}
}
