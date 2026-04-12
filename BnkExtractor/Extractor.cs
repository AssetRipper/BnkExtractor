using BnkExtractor.Ww2ogg;
using System.Collections.Generic;
using System.IO;

namespace BnkExtractor
{
    public class Extractor
    {
        public static void ParseBnk(string filePath) => BnkExtr.BnkParser.Parse(filePath, false, false, false);
        public static void RevorbOgg(string inputFilePath) => Revorb.RevorbSharp.Convert(inputFilePath, null);
        public static void ConvertWem(string filePath)
        {
            Ww2oggOptions options = new Ww2oggOptions();
            options.InFilename = filePath;
            options.OutFilename = Path.ChangeExtension(filePath, "ogg");
            options.CodebooksFilename = "packed_codebooks_aoTuV_603.bin";
            Ww2oggConverter.Main(options);
        }

        /// <summary>
        /// Extracts BNK, converts all WEM to Revorbed Oggs, writes to disk
        /// </summary>
        /// <param name="filePath">Input .bnk file path</param>
        /// <param name="noDirectory">Optionally create subdirectory for ogg files</param>
        public static void BnkToOgg(string filePath, bool noDirectory = false)
        {
            Ww2oggOptions options = new Ww2oggOptions();
            options.CodebooksFilename = "packed_codebooks_aoTuV_603.bin";
            Dictionary<uint, MemoryStream> wemFiles = BnkExtr.BnkParser.ParseToMemory(filePath, false, noDirectory, true);
            Dictionary<uint, MemoryStream> oggFiles = Ww2oggConverter.Main(wemFiles, options);

            string outDirectory = noDirectory
                ? Path.GetDirectoryName(filePath)
                : BnkExtr.BnkParser.CreateOutputDirectory(filePath);

            foreach (var (key, oggFile) in oggFiles) 
            {
                var outFile = Path.Combine(outDirectory, $"{key}.ogg");
                using var fs = new FileStream(outFile, FileMode.Create, FileAccess.Write);
                Revorb.RevorbSharp.Convert(oggFile, fs);
            }
        }

        /// <summary>
        /// Extracts BNK and converts all WEM to Revorbed Oggs, keeps in memory
        /// </summary>
        /// <param name="filePath">Input .bnk file path</param>
        public static Dictionary<uint, MemoryStream> BnkToOggMemory(string filePath)
        {
            Ww2oggOptions options = new Ww2oggOptions();
            options.CodebooksFilename = "packed_codebooks_aoTuV_603.bin";
            Dictionary<uint, MemoryStream> wemFiles = BnkExtr.BnkParser.ParseToMemory(filePath, false, true, false);
            Dictionary<uint, MemoryStream> oggFiles = Ww2oggConverter.Main(wemFiles, options);

            Dictionary<uint, MemoryStream> result = new Dictionary<uint, MemoryStream>();

            foreach (var (key, oggStream) in oggFiles)
            {
                var outStream = new MemoryStream();
                Revorb.RevorbSharp.Convert(oggStream, outStream, true);
                outStream.Position = 0;
                result.Add(key, outStream);
                oggStream.Dispose();
            }

            return result;
        }
    }
}
