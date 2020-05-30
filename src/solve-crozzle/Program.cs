using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using crozzle;

namespace solve_crozzle
{
	class Program
	{




		static int Main(string[] args)
		{
			try
			{
				var parameters = Parameters.Parse(args);
				var words = CrozzleFileReader.ExtractWords(parameters.FilePath).Result;
				Workspace workspace = Workspace.Generate(words);
				var workspaces = words
					.Select(w => workspace.PlaceWord(Direction.Across, w, 0, 0))
					.ToArray();

				ulong generatedSolutionsCount = 0;
				var maxScore = 0;
				DateTime timeStart = DateTime.Now;
				Console.WriteLine($"*** Run started {timeStart.ToShortDateString()} {timeStart.ToLongTimeString()}");
				Console.WriteLine($"*** Input file: {parameters.FilePath} ***");
				Console.WriteLine($"*** BeamSize: {parameters.BeamSize} ***");
				//foreach (var thisWorkspace in Runner.SolveUsingQueue(
				//	workspaces,
				//	10000000,
				//	parameters.BeamSize,
				//	new CancellationTokenSource().Token
				//))
				foreach(var thisWorkspace in Runner.SolveUsingSimpleRecursion(
					workspaces.First(),
					new SpaceFillingNextStepGenerator(
						new SpaceFillingGenerationSettings
						{
							MaxContiguousSpaces = 6,
						}
					),
					new CancellationTokenSource().Token)
				)
				{
					++generatedSolutionsCount;
					if (thisWorkspace.Score > maxScore)
					{
						TimeSpan duration = DateTime.Now - timeStart;
						maxScore = thisWorkspace.Score;
						Console.WriteLine(thisWorkspace.BoardRepresentation);
						Console.WriteLine(thisWorkspace.GenerateScoreBreakdown());
						Console.WriteLine($"*** {duration}:  {generatedSolutionsCount:n0} solutions generated. ({generatedSolutionsCount / duration.TotalSeconds:n0} per second) ***");
					}
				}
				return 0;
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
				return -1;
			}
		}
	}
}
