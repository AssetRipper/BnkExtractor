using System;

namespace BnkExtractorConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			if(args.Length != 1)
			{
				Console.WriteLine("Requires exactly one argument");
			}
			else
			{
				try
				{
					switch (System.IO.Path.GetExtension(args[0]))
					{
						case ".bnk":
							BnkExtractor.Extractor.ParseBnk(args[0]);
							break;
						case ".ogg":
							BnkExtractor.Extractor.RevorbOgg(args[0]);
							break;
						case ".wem":
							BnkExtractor.Extractor.ConvertWem(args[0]);
							break;
						default:
							Console.WriteLine($"No support available for {args[0]}");
							break;
					}
					
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
				}
			}
			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
