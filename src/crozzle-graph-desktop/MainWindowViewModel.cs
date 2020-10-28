using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace crozzle_graph_desktop
{
    using crozzle;
	using crozzle_graph;
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

		private async Task DoStuff()
		{
			WordDatabase wordDatabase = await GenerateWordDatabaseAsync();
			var intersections = IntersectionBuilder.GetIntersections(wordDatabase).ToArray();
			int l = intersections.Length;
		}

		public MainWindowViewModel()
		{
			var task = DoStuff();
		}
	}
}
