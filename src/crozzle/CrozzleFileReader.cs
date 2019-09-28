using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace crozzle
{
	public static class CrozzleFileReader
	{
		public static async Task<List<string>> ExtractWords(string filePath)
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
	}
}
