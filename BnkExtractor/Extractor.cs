using BnkExtractor.Ww2ogg;
using System.IO;

namespace BnkExtractor
{
	public class Extractor
	{
		public static void ParseBnk(string filePath) => BnkExtr.BnkParser.Parse(filePath, false, false, false);
		public static void RevorbOgg(string filePath) => Revorb.RevorbSharp.Convert(filePath, null);
		public static void ConvertWem(string filePath)
        {
			Ww2oggOptions options = new Ww2oggOptions();
			options.InFilename = filePath;
			options.OutFilename = Path.ChangeExtension(filePath, "ogg");
			options.CodebooksFilename = "packed_codebooks_aoTuV_603.bin";
			Ww2oggConverter.Main(options);
        }
	}
}
