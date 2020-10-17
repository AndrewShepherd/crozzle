namespace crozzle
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;

	public static class WordStreamReader
	{
		public static async Task<IEnumerable<string>> Read(Stream stream)
		{
			var words = new List<string>();
			using (StreamReader sr = new StreamReader(stream))
			{
				while (!sr.EndOfStream)
				{
					var line = await sr.ReadLineAsync();
					if (!String.IsNullOrWhiteSpace(line))
					{
						words.Add(line.Trim());
					}
				}
			}
			return words;
		}
	}
}
