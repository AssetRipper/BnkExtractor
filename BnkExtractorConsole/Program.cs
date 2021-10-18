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
					BnkExtractor.Extractor.ParseBnk(args[0]);
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
				}
			}
			Console.ReadLine();
		}
	}
}
