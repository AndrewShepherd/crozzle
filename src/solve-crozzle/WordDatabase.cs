using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace solve_crozzle
{
    public class WordDatabase
	{
		public ImmutableHashSet<string> AvailableWords = ImmutableHashSet<string>.Empty;
		public Dictionary<string, ImmutableHashSet<String>> WordLookup = new Dictionary<string, ImmutableHashSet<string>>();

		internal WordDatabase Remove(string word) =>
			new WordDatabase
			{
				AvailableWords = this.AvailableWords.Remove(word),
				WordLookup = this.WordLookup
			};
	}
}
