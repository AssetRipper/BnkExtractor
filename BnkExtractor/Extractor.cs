using System;

namespace BnkExtractor
{
	public class Extractor
	{
		public static void ParseBnk(string filePath) => BnkExtr.BnkParser.Parse(filePath, false, false, false);
	}
}
