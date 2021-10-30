using BnkExtractor.Ww2ogg.Exceptions;
using System;
using System.IO;

namespace BnkExtractor.Ww2ogg;

public static class Ww2oggConverter
{
    public static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("usage: ww2ogg input.wav [-o output.ogg] [--inline-codebooks] [--full-setup]");
        Console.WriteLine("                        [--mod-packets | --no-mod-packets]");
        Console.WriteLine("                        [--pcb packed_codebooks.bin]");
        Console.WriteLine();
    }

    internal static int Main(int argc, string[] args)
    {
        Console.WriteLine($"Audiokinetic Wwise RIFF/RIFX Vorbis to Ogg Vorbis converter");
        Console.WriteLine();

        Ww2oggOptions opt = new Ww2oggOptions();

        try
        {
            opt.ParseArguments(argc, args);
        }
        catch (ArgumentError ae)
        {
            Console.WriteLine(ae);

            PrintUsage();
            return 1;
        }
        return Main(opt);
    }

    internal static int Main(Ww2oggOptions opt)
    {
        try
        {
            Console.Write("Input: ");
            Console.Write(opt.InFilename);
            Console.WriteLine();
            Wwise_RIFF_Vorbis ww = new Wwise_RIFF_Vorbis(opt.InFilename, opt.CodebooksFilename, opt.InlineCodebooks, opt.FullSetup, opt.ForcePacketFormat);

            ww.PrintInfo();
            Console.Write("Output: ");
            Console.Write(opt.OutFilename);
            Console.WriteLine();

            BinaryWriter of = new BinaryWriter(File.Create(opt.OutFilename));
            if (of == null)
            {
                throw new FileOpenException(opt.OutFilename);
            }

            ww.GenerateOgg(of);
            Console.Write("Done!");
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (FileOpenException fe)
        {
            Console.Write(fe);
            Console.WriteLine();
            return 1;
        }
        catch (ParseException pe)
        {
            Console.Write(pe);
            Console.WriteLine();
            return 1;
        }
        return 0;
    }

}
