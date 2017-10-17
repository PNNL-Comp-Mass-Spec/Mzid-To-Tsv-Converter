using System;
using System.IO;
using PRISM;

namespace MzidToTsvConverter
{
    public class ConverterOptions
    {
        public ConverterOptions()
        {
            MzidPath = "";
            TsvPath = "";
            UnrollResults = false;
            ShowDecoy = false;
            SingleResultPerSpectrum = false;
        }

        [Option("mzid", Required = true, HelpText = "Path to mzid[.gz] file; if path has spaces, it must be in quotes.")]
        public string MzidPath { get; set; }

        [Option("tsv", HelpText = "Path to tsv file to be written; if not specified, will be output to the same location as the mzid")]
        public string TsvPath { get; set; }

        [Option("unroll", "u", HelpText = "Unroll the results - output one line per unique peptide/protein combination in each spectrum identification", HelpShowsDefault = true)]
        public bool UnrollResults { get; set; }

        [Option("showDecoy", "sd", HelpText = "Include decoy results in the result tsv", HelpShowsDefault = true)]
        public bool ShowDecoy { get; set; }

        [Option("singleResult", "1", HelpText = "Only output one result per spectrum", HelpShowsDefault = true)]
        public bool SingleResultPerSpectrum { get; set; }

        public string AutoNameTsvFromMzid(string mzidPath)
        {
            var path = mzidPath;
            if (path.ToLower().EndsWith(".gz"))
            {
                path = Path.ChangeExtension(path, null);
            }
            return Path.ChangeExtension(path, "tsv");
        }

        public bool HasWildcard(string filePath)
        {
            return filePath.Contains("*") || filePath.Contains("?");
        }

        public bool ValidateArgs()
        {
            if (string.IsNullOrWhiteSpace(MzidPath))
            {
                Console.WriteLine("ERROR: mzid path must be specified!");
                return false;
            }

            if (!HasWildcard(MzidPath))
            {
                var mzidFile = new FileInfo(MzidPath);
                if (!mzidFile.Exists)
                {
                    Console.WriteLine("ERROR: mzid file does not exist!");
                    Console.WriteLine(mzidFile.FullName);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(TsvPath))
            {
                TsvPath = AutoNameTsvFromMzid(MzidPath);
            }

            return true;
        }

        public void OutputSetOptions()
        {
            Console.WriteLine("Using options:");
            Console.WriteLine("mzid path: \"{0}\"", MzidPath);
            Console.WriteLine("tsv path: \"{0}\"", TsvPath);
            Console.WriteLine("unroll results: {0}", UnrollResults);
            Console.WriteLine("show decoy: {0}", ShowDecoy);
            Console.WriteLine("single result per spectrum: {0}", SingleResultPerSpectrum);
        }

    }
}