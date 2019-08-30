using System;
using System.Collections.Generic;
using System.Text;

namespace solve_crozzle
{
	class Parameters
	{
		public string FilePath { get; private set; }

		public int BeamSize { get; private set; } = 256;

		public static Parameters Parse(string[] args)
		{
			var parameters = new Parameters();
			for(int i = 0; i < args.Length; ++i)
			{
				if(args[i].Equals("--wordsfile", StringComparison.InvariantCultureIgnoreCase))
				{
					var filePath = args[++i];
					if(!System.IO.File.Exists(filePath))
					{
						throw new Exception($"Cannot find the file '{filePath}'");
					}
					parameters.FilePath = filePath;
				}
				else if (args[i].Equals("--beamsize", StringComparison.InvariantCultureIgnoreCase))
				{
					int beamSize;
					if(!int.TryParse(args[++i], out beamSize))
					{
						throw new Exception("Invalid beamsize value. Value must be a positive integer");
					}
					parameters.BeamSize = beamSize;
				}
			}
			if(string.IsNullOrEmpty(parameters.FilePath))
			{
				throw new Exception("Error: No input file specified. Usage: solve-crozzle --wordsfile <filepath>");
			}
			return parameters;
		}
	}
}
