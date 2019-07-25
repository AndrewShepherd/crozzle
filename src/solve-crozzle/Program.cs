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

		static IEnumerable<Workspace> SolveUsingQueue(IEnumerable<Workspace> startWorkspaces, int queueLength)
		{
			WorkspacePriorityQueue wpq = new WorkspacePriorityQueue(queueLength);
			foreach (var workspace in startWorkspaces)
			{
				wpq.Push(workspace);
			}
			while (!wpq.IsEmpty)
			{
				var thisWorkspace = wpq.Pop();
				var nextSteps = thisWorkspace.GenerateNextSteps().ToList();
				if (nextSteps.Any())
				{
					wpq.AddRange(nextSteps);
				}
				else
				{
					if (thisWorkspace.IsValid)
					{
						yield return thisWorkspace;
					}
				}
			}
		}


		static void Main(string[] args)
		{
			DateTime timeStart = DateTime.Now;
			var words = ExtractWords(DefaultFilePath).Result;
			Workspace workspace = Workspace.Generate(words);
			var workspaces = words
				.Select(
					w =>
						new[]
						{
							workspace.PlaceWord(Direction.Across, w, 0, 0),
							workspace.PlaceWord(Direction.Down, w, 0, 0),
						}
				).SelectMany(_ => _);
			int maxScore = 0;
			int generatedSolutionsCount = 0;
			foreach(var thisWorkspace in SolveUsingQueue(workspaces, 10000000))
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
