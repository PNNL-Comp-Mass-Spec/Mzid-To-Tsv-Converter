using System;
using System.Collections.Generic;
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
            IsDirectory = false;
            MaxSpecEValue = 0;
            MaxEValue = 0;
            MaxQValue = 0;
            NoExtendedFields = false;
        }

        [Option("mzid", Required = true, ArgPosition = 1, HelpText = "Path to .mzid or .mzid.gz file; if path has spaces, it must be in quotes. Can also provide a directory to convert all .mzid[.gz] files in the directory.")]
        public string MzidPath { get; set; }

        [Option("tsv", ArgPosition = 2, HelpText = "Path to tsv file to be written; if not specified, will be output to the same location as the mzid. If mzid path is a directory, this will be treated as a directory path (which must exist).")]
        public string TsvPath { get; set; }

        [Option("unroll", "u",
            HelpText = "Unroll the results - output one line per unique peptide/protein combination in each spectrum identification",
            HelpShowsDefault = true)]
        public bool UnrollResults { get; set; }

        [Option("maxSpecEValue", "MaxSpecE", "SpecEValue", HelpText = "Maximum SpecEValue filter (ignored if 0 or 1)", HelpShowsDefault = true, Min = 0, Max = 1)]
        public double MaxSpecEValue { get; set; }

        [Option("maxEValue", "MaxE", "EValue", HelpText = "Maximum EValue filter (ignored if 0)", HelpShowsDefault = true, Min = 0, Max = float.MaxValue, DefaultValueFormatString = "(Default: {0} Min: {1} Max: {2:0.0E+0})")]
        public double MaxEValue { get; set; }

        [Option("maxQValue", "MaxQ", "QValue", HelpText = "Maximum QValue filter (ignored if 0 or 1)", HelpShowsDefault = true, Min = 0, Max = 1)]
        public double MaxQValue { get; set; }

        [Option("showDecoy", "sd", HelpText = "Include decoy results in the result tsv", HelpShowsDefault = true)]
        public bool ShowDecoy { get; set; }

        [Option("singleResult", "1", HelpText = "Only output one result per spectrum", HelpShowsDefault = true)]
        public bool SingleResultPerSpectrum { get; set; }

        [Option("skipDupIds", HelpText = "If there are issues converting a file due to \"duplicate ID\" errors, specifying this will cause the duplicate IDs to be ignored, at the likely cost of some correctness.", HelpShowsDefault = true)]
        public bool SkipDuplicateIds { get; set; }

        [Option("r", "recurse", HelpText = "If mzid path is a directory, specifying this will cause mzid files in subdirectories to also be converted.")]
        public bool RecurseDirectories { get; set; }

        [Option("ne", "noExtended", HelpText = "If specified, does not add extended fields to the TSV output (e.g., scan time).")]
        public bool NoExtendedFields { get; set; }

        public bool IsDirectory { get; private set; }

        public List<string> MzidPaths { get; } = new List<string>();

        public string AutoNameTsvFromMzid(string mzidPath)
        {
            var path = mzidPath;
            if (path.ToLower().EndsWith(".gz"))
            {
                path = Path.ChangeExtension(path, null);
            }

            if (IsDirectory && !string.IsNullOrWhiteSpace(TsvPath))
            {
                path = Path.Combine(TsvPath, Path.GetFileName(path));
            }

            return Path.ChangeExtension(path, "tsv");
        }

        /// <summary>
        /// Returns true if filterThreshold is greater than 0 but less than 1
        /// </summary>
        /// <param name="filterThreshold"></param>
        /// <returns></returns>
        public bool FilterEnabled(double filterThreshold)
        {
            return filterThreshold > 0 && filterThreshold < 1;
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
                    if (Directory.Exists(MzidPath))
                    {
                        IsDirectory = true;
                    }
                    else
                    {
                        Console.WriteLine("ERROR: mzid file does not exist!");
                        Console.WriteLine(mzidFile.FullName);
                        return false;
                    }
                }
            }

            if (IsDirectory)
            {
                var searchOption = RecurseDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var mzidFiles = Directory.GetFileSystemEntries(MzidPath, "*.mzid", searchOption);
                var mzidGzFiles = Directory.GetFileSystemEntries(MzidPath, "*.mzid.gz", searchOption);
                if (mzidFiles.Length > 0 || mzidGzFiles.Length > 0)
                {
                    MzidPaths.AddRange(mzidFiles);
                    // Check for extracted .gz files - if they have been extracted, don't convert the .gz file
                    // Not checking for identically named files in different directories, but that is a possible consideration for the user if they specify "recurse"
                    var newFiles = new List<string>();
                    foreach (var gzFile in mzidGzFiles)
                    {
                        var noGz = Path.ChangeExtension(gzFile, null); // remove the .gz extension
                        if (!MzidPaths.Contains(noGz))
                        {
                            newFiles.Add(gzFile);
                        }
                    }
                    MzidPaths.AddRange(newFiles);
                }
            }

            if (IsDirectory)
            {
                if (!string.IsNullOrWhiteSpace(TsvPath) && !Directory.Exists(TsvPath))
                {
                    Console.WriteLine("ERROR: mzid path is a directory, but tsv path is not an existing directory!");
                    Console.WriteLine("Correct the tsv path or create directory \"{0}\"!", TsvPath);
                    return false;
                }
            }
            else if (string.IsNullOrWhiteSpace(TsvPath))
            {
                TsvPath = AutoNameTsvFromMzid(MzidPath);
            }

            return true;
        }

        public void OutputSetOptions()
        {
            Console.WriteLine("Using options:");
            if (!IsDirectory)
            {
                Console.WriteLine("mzid path: \"{0}\"", MzidPath);
                Console.WriteLine("tsv path: \"{0}\"", TsvPath);
            }
            else
            {
                Console.WriteLine("mzid directory: \"{0}\"{1}", MzidPath, RecurseDirectories ? " and subdirectories" : "");
                if (!string.IsNullOrWhiteSpace(TsvPath))
                {
                    Console.WriteLine("tsv directory: \"{0}\"", TsvPath);
                }
                Console.WriteLine("mzid and tsv paths:");
                foreach (var path in MzidPaths)
                {
                    Console.WriteLine("\t{0}", path);
                    Console.WriteLine("\t\t{0}", AutoNameTsvFromMzid(path));
                }
            }

            Console.WriteLine("Unroll results: {0}", UnrollResults);
            Console.WriteLine("Show decoy: {0}", ShowDecoy);
            Console.WriteLine("Single result per spectrum: {0}", SingleResultPerSpectrum);

            if (SkipDuplicateIds)
            {
                Console.WriteLine("Skipping duplicate IDs");
            }

            if (FilterEnabled(MaxSpecEValue) || MaxEValue > 0 || FilterEnabled(MaxQValue))
            {
                Console.WriteLine("Filtering results by score");
                if (FilterEnabled(MaxSpecEValue))
                {
                    Console.WriteLine("Max SpecEValue: {0}", StringUtilities.DblToString(MaxSpecEValue, 5, 0.00005));
                }

                if (MaxEValue > 0)
                {
                    Console.WriteLine("Max EValue: {0}", StringUtilities.DblToString(MaxEValue, 5, 0.00005));
                }

                if (FilterEnabled(MaxQValue))
                {
                    Console.WriteLine("Max QValue: {0}", StringUtilities.DblToString(MaxQValue, 5, 0.00005));
                }
            }
            else
            {
                Console.WriteLine("No filters are in use");
            }

        }

    }
}