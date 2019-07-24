using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace solve_crozzle
{
	class Program
	{





		static string DefaultFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\8803.txt";

		static async Task<List<string>> ExtractWords(string filePath)
		{
			List<string> listString = new List<string>();
			using (StreamReader sr = new StreamReader(filePath))
			{
				var s = await sr.ReadLineAsync();
				while (s != null)
				{
					listString.Add(s);
					s = await sr.ReadLineAsync();
				}
			}
			return listString;
		}

		

		static void Main(string[] args)
		{
			DateTime timeStart = DateTime.Now;
			var words = ExtractWords(DefaultFilePath).Result;

			Workspace workspace = Workspace.Generate(words);
			

			WorkspacePriorityQueue wpq = new WorkspacePriorityQueue();
			foreach(var s in words)
			{
				wpq.Push(workspace.PlaceWord(Direction.Across, s, 0, 0));
				wpq.Push(workspace.PlaceWord(Direction.Down, s, 0, 0));
			}
			int maxScore = 0;
			int generatedSolutionsCount = 0;
			while(!wpq.IsEmpty)
			{
				var thisWorkspace = wpq.Pop();
				var nextSteps = thisWorkspace.GenerateNextSteps().ToList();
				if (nextSteps.Any())
				{
					wpq.AddRange(nextSteps);
				}
				else
				{
					if(thisWorkspace.IsValid)
					{
						++generatedSolutionsCount;
						if (thisWorkspace.Score > maxScore)
						{
							TimeSpan duration = DateTime.Now - timeStart;
							maxScore = thisWorkspace.Score;
							Console.WriteLine($"*** {duration}: MaxScore is {maxScore}, {generatedSolutionsCount} solutions generated ***");
							Console.WriteLine(thisWorkspace.BoardRepresentation);
							Console.WriteLine(thisWorkspace.GenerateScoreBreakdown());
						}
					}
				}
			}
		}
	}
}
