﻿namespace crozzle
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	public class WordDatabase
	{
		private string[] _wordArray;
		private Dictionary<string, int> _wordArrayIndex;
		private BitArray _wordAvailability;
		private Dictionary<string, List<int>> WordLookup = new Dictionary<string, List<int>>();

		internal static WordDatabase Empty = WordDatabase.Generate(Enumerable.Empty<string>());

		private WordDatabase()
		{
		}

		public WordDatabase Remove(string word)
		{
			var wordAvailability = (BitArray)_wordAvailability.Clone();
			wordAvailability.Set(_wordArrayIndex[word], false);
			return new WordDatabase
			{
				_wordArray = this._wordArray,
				_wordArrayIndex = this._wordArrayIndex,
				_wordAvailability = wordAvailability,
				WordLookup = this.WordLookup
			};
		}

		public override int GetHashCode()
		{
			int hash = 0;
			for(int i = 0; i < _wordAvailability.Length; ++i)
			{
				hash ^= HashUtils.RotateLeft(_wordAvailability[i] ? 1 : 0, i%32);
			}
			return hash;
		}

		public override bool Equals(object obj)
		{
			if(object.ReferenceEquals(this, obj))
			{
				return true;
			}
			if(!(obj is WordDatabase wd))
			{
				return false;
			}
			for(int i = 0; i < _wordAvailability.Length; ++i)
			{
				if(this._wordAvailability[i] != wd._wordAvailability[i])
				{
					return false;
				}
			}
			return true;
		}

		public static WordDatabase Generate(IEnumerable<string> words)
		{

			var wordDatabase = new WordDatabase();
			wordDatabase._wordArray = words.ToArray();
			wordDatabase._wordArrayIndex = new Dictionary<string, int>();
			wordDatabase._wordAvailability = new BitArray(wordDatabase._wordArray.Length);
			wordDatabase._wordAvailability.SetAll(true);
			for(int arrayIndex = 0; arrayIndex < wordDatabase._wordArray.Length; ++arrayIndex)	
			{
				string word = wordDatabase._wordArray[arrayIndex];
				wordDatabase._wordArrayIndex[word] = arrayIndex;
				for (int i = 0; i < word.Length; ++i)
				{
					for (int j = 1; j + i <= word.Length; ++j)
					{
						var substring = word.Substring(i, j);
						if (wordDatabase.WordLookup.TryGetValue(substring, out var existingList))
						{
							if (!existingList.Contains(arrayIndex))
							{
								existingList.Add(arrayIndex);
							}
						}
						else
						{
							wordDatabase.WordLookup[word.Substring(i, j)] = new List<int>(new[] { arrayIndex });
						}
					}
				}
			}
			return wordDatabase;
		}

		public IEnumerable<string> ListAvailableMatchingWords(string word)
		{
			if (this.WordLookup.TryGetValue(word, out var wordIndexList))
			{
				foreach(var wordIndex in wordIndexList)
				{
					if(this._wordAvailability[wordIndex])
					{
						yield return this._wordArray[wordIndex];
					}
				}
			}
		}

		public bool CanMatchWord(string word) =>
			ListAvailableMatchingWords(word).Any();
	}
}