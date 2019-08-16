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
		static string WinterFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\8908.txt";
		static string HeavyOverlapFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\heavyoverlap.TXT";
		static string MountainsFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\9312.TXT";
		static string ChristmasFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\8912.TXT";
		static string TownsFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\9003.TXT";
		static string AugustFilePath = @"C:\Users\sheph\Documents\GitHub\crozzle\wordlists\20190814.TXT";

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

		static IEnumerable<Workspace> SolveRecursively(IEnumerable<Workspace> workspaces)
		{
			foreach(var w in workspaces)
			{
				var nextSteps = w.GenerateNextSteps()
					.ToList();
				if(!nextSteps.Any())
				{
					if (w.IsValid)
						yield return w;
				}
				else
				{
					foreach (
						var w2 in SolveRecursively(
							nextSteps.OrderByDescending(ns => ns.PotentialScore)
						)
					)
						yield return w2;
				}
			}
		}



		public static bool IsMatch<T>(T[] left, T[] right)
		{
			if (left.Length != right.Length)
				return false;
			for(int i = 0; i < left.Length; ++i)
			{
				if (!(left[i].Equals(right[i])))
					return false;
			}
			return true;
		}

		public enum OverflowPolicy {  SolveRecursively, Discard };
		static IEnumerable<Workspace> SolveUsingQueue(IEnumerable<Workspace> startWorkspaces, int queueLength, int batchSize, OverflowPolicy overflowPolicy)
		{
			WorkspacePriorityQueue wpq = new WorkspacePriorityQueue(queueLength);
			foreach (var workspace in startWorkspaces)
			{
				wpq.Push(workspace);
			}

			while (!wpq.IsEmpty)
			{
				List<Workspace> wList = new List<Workspace>();
				wList.Add(wpq.Pop());
				while (wList.Count < batchSize && !wpq.IsEmpty)
				{
					var poppedValue = wpq.Pop();
					if (wList[wList.Count - 1].Equals(poppedValue))
					{
						int dummy = 3;
					}
					else
					{
						wList.Add(poppedValue);
					}
				}
				foreach (var thisWorkspace in wList)
				{
					var nextSteps = thisWorkspace.GenerateNextSteps().ToList();
					if (nextSteps.Any())
					{
						foreach (var ns in nextSteps)
						{
							if (ns.IsValid)
							{
								wpq.Push(ns);
							}
							else
							{
								foreach (var nsChild in GetValidChildren(ns))
								{
									wpq.Push(nsChild);
								}
							}
						}
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
		}

		public static IEnumerable<Tuple<T, T>> GetPairs<T>(IEnumerable<T> list)
			=> list
				.Select(
					(t, n) => list.Skip(n + 1).Select(t2 => Tuple.Create(t, t2))
				).SelectMany(_ => _);

		public static IEnumerable<Workspace> GetValidChildren(Workspace workspace)
		{
			foreach(var nextStep in workspace.GenerateNextSteps())
			{
				if(nextStep.IsValid)
				{
					yield return nextStep;
				}
				else
				{
					foreach(var child in GetValidChildren(nextStep))
					{
						yield return child;
					}
				}
			}
		}

		public static IEnumerable<Workspace> GetClusters(IEnumerable<String> words)
		{
			var sourceWorkspace = Workspace.Generate(words);
			var pairs = GetPairs(words).ToArray();
			Console.WriteLine($"There are {pairs.Length} pairs to test");
			for(int ip = 0; ip < pairs.Length; ++ip)
			{
				//Console.WriteLine($"Testing pair {ip} of {pairs.Length}");
				var pair = pairs[ip];
				for(
					int i = 0 - pair.Item2.Length+2;
					i < pair.Item1.Length-1;
					++i
				)
				{
					var w = sourceWorkspace.PlaceWord(Direction.Across, pair.Item1, 0, 0);
					w = w.PlaceWord(Direction.Across, pair.Item2, i, 1);
					foreach (var validChild in GetValidChildren(w))
					{
						yield return validChild;
					}
				}



				for (
					int i = 0 - pair.Item2.Length + 2;
					i < pair.Item1.Length-1;
					++i
				)
				{
					var w = sourceWorkspace.PlaceWord(Direction.Down, pair.Item1, 0, 0);
					w = w.PlaceWord(Direction.Down, pair.Item2, 1, i);
					foreach (var validChild in GetValidChildren(w))
					{
						yield return validChild;
					}
				}
			}
		}

		static List<Workspace> GetClusteredWorkspaces(IEnumerable<string> words)
		{
			List<Workspace> workspaces = new List<Workspace>();
			int c = 0;
			var clustered = GetClusters(words).ToList();
			var distinctClusters = new List<Workspace>();

			foreach (var clusteredWorkspace in GetClusters(words))
			{
				if (!distinctClusters.Any(
					dc => IsMatch(dc.Board.Values, clusteredWorkspace.Board.Values))
				)
				{
					Console.WriteLine("Cluster found!");
					Console.WriteLine(clusteredWorkspace.BoardRepresentation);
					workspaces.Add(clusteredWorkspace);
					++c;
				}
			}
			Console.WriteLine($"Found {c} clusters");
			return workspaces;
		}


		static void Main(string[] args)
		{
			//var words = ExtractWords(HeavyOverlapFilePath).Result;
			var words = ExtractWords(AugustFilePath).Result;
			Workspace workspace = Workspace.Generate(words);
			var workspaces = new[]
			{
				workspace.PlaceWord(Direction.Down,"QUIZ", 0, 0),
				workspace.PlaceWord(Direction.Across,"QUIZ", 0, 0),
			};



			ulong generatedSolutionsCount = 0;
			var maxScore = 0;
			DateTime timeStart = DateTime.Now;
			foreach (var thisWorkspace in SolveUsingQueue(workspaces, 2000000, 32, OverflowPolicy.Discard))
			{
				++generatedSolutionsCount;
				if (thisWorkspace.Score > maxScore)
				{
					TimeSpan duration = DateTime.Now - timeStart;
					maxScore = thisWorkspace.Score;
					Console.WriteLine(thisWorkspace.BoardRepresentation);
					Console.WriteLine(thisWorkspace.GenerateScoreBreakdown());
					Console.WriteLine($"*** {duration}:  {generatedSolutionsCount:n0} solutions generated. ({generatedSolutionsCount/duration.TotalSeconds:n0} per second) ***");
				}
			}
		}
	}
}
