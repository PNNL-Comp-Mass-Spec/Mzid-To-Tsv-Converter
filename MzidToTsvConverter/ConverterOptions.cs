using System;
using System.Collections.Generic;
using System.IO;
using Arguments;

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
        }

        public string MzidPath { get; set; }
        public string TsvPath { get; set; }
        public bool UnrollResults { get; set; }
        public bool ShowDecoy { get; set; }

        public void ShowUsage()
        {
            Console.WriteLine("Mzid to Tsv Converter");
            Console.WriteLine("Usage: {0} -mzid:\"mzid path\" [-tsv:\"tsv output path\"] [-unroll|-u] [-showDecoy|-sd]", System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
            Console.WriteLine("  Required parameters:");
            Console.WriteLine("\t'-mzid:path' - path to mzid[.gz] file; if path has spaces, it must be in quotes.");
            Console.WriteLine("  Optional parameters:");
            Console.WriteLine("\t'-tsv:path' - path to tsv file to be written; if not specified, will be output to same location as mzid");
            Console.WriteLine("\t'-unroll|-u' signifies that results should be unrolled - one line per unique peptide/protein combination in each spectrum identification");
            Console.WriteLine("\t'-showDecoy|-sd' signifies that decoy results should be included in the result tsv");

            System.Threading.Thread.Sleep(1500);
        }

        public bool ProcessArgs(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return false;
            }

            var argProc = ArgumentProcessor.Initialize(args, new List<IArgument>())
                .UsingParameterSeparator(':')
                .AddArgument("mzid").WithAction(param => { MzidPath = param; })
                .AddArgument("tsv").WithAction(param => { TsvPath = param; })
                .AddArgument("unroll", "u").WithAction(param => { UnrollResults = true; })
                .AddArgument("showDecoy", "sd").WithAction(param => { ShowDecoy = true; })
                .Process();
            
            if (ValidateArgs())
                return true;

            Console.WriteLine();
            ShowUsage();
            return false;
        }

        private bool ValidateArgs()
        {
            if (string.IsNullOrWhiteSpace(MzidPath))
            {
                Console.WriteLine("ERROR: mzid path must be specified!");
                return false;
            }

            var mzidFile = new FileInfo(MzidPath);
            if (!mzidFile.Exists)
            {
                Console.WriteLine("ERROR: mzid file does not exist!");
                Console.WriteLine(mzidFile.FullName);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TsvPath))
            {
                var path = MzidPath;
                if (path.ToLower().EndsWith(".gz"))
                {
                    path = Path.ChangeExtension(path, null);
                }
                TsvPath = Path.ChangeExtension(path, "tsv");
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
        }
    }
}