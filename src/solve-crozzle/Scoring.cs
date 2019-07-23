using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	static class Scoring
	{
		private static IDictionary<char, int> GenerateIntersectionScores()
		{
			var d = new Dictionary<char, int>();
			for (char c = 'a'; c <= 'f'; ++c)
				d[c] = 2;
			for (char c = 'g'; c <= 'l'; ++c)
				d[c] = 4;
			for (char c = 'm'; c <= 'r'; ++c)
				d[c] = 8;
			for (char c = 's'; c <= 'x'; ++c)
				d[c] = 16;
			d['y'] = 32;
			d['z'] = 64;
			return d;
		}

		public const int ScorePerWord = 10;

		static IDictionary<char, int> _intersectionScores = GenerateIntersectionScores();

		public static int Score(char c) =>
			_intersectionScores[char.ToLowerInvariant(c)];
		public static int Score(string s) =>
			s.Select(c => Score(c)).Sum();
	}
}
