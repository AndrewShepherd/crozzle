using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	public enum Direction {  Across, Down };

	public class Slot
	{
		public Direction Direction;
		public char Letter;
		public int X;
		public int Y;
	}

	public class PartialWord
	{
		public Direction Direction;
		public string Value;
		public int X;
		public int Y;
	}

	public class Workspace
	{
		public int Score = 0;
		public int Width = 0;
		public int Height = 0;
		public int XStart = 0;
		public int YStart = 0;
		public char[] Values = new char[0];
		public ulong AvailableWords = 0;

		public ImmutableList<Slot> Slots = ImmutableList<Slot>.Empty;
		public ImmutableList<PartialWord> PartialWords = ImmutableList<PartialWord>.Empty;

		internal static Workspace Generate(IEnumerable<string> words)
		{
			ulong wordFlag = 0x1;
			var workspace = new Workspace()
			{
				WordToFlagMap = new Dictionary<string, ulong>(),
				FlagToWordMap = new Dictionary<ulong, string>(),
				WordLookup = new Dictionary<string, List<ulong>>()
			};
			foreach (var word in words)
			{
				workspace.AvailableWords |= wordFlag;
				workspace.WordToFlagMap[word] = wordFlag;
				workspace.FlagToWordMap[wordFlag] = word;
				for (int i = 0; i < word.Length; ++i)
				{
					for (int j = 1; j + i <= word.Length; ++j)
					{
						var substring = word.Substring(i, j);
						if (workspace.WordLookup.TryGetValue(substring, out var existingList))
						{
							if (!existingList.Contains(wordFlag))
								existingList.Add(wordFlag);
						}
						else
							workspace.WordLookup[word.Substring(i, j)] = new List<ulong> { wordFlag };
					}
				}
				wordFlag = wordFlag << 1;
			}
			return workspace;
		}

		public Dictionary<string, ulong> WordToFlagMap = new Dictionary<string, ulong>();
		public Dictionary<ulong, string> FlagToWordMap = new Dictionary<ulong, string>();

		public Dictionary<string, List<ulong>> WordLookup = new Dictionary<string, List<ulong>>();

		public bool IsValid => PartialWords.IsEmpty;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for(int i = 0; i < Values.Length; ++i)
			{
				sb.Append(Values[i] == (char)0 ? ' ' : Values[i]);
				if(i % Width == Width-1)
				{
					sb.AppendLine();
				}
			}
			return sb.ToString();
		}
	}
}
