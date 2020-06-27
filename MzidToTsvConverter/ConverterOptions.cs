using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PRISM;

namespace MzidToTsvConverter
{
    public class ConverterOptions
    {
        /// <summary>
        /// Default RegEx for matching gene names
        /// Supports the UniProt SwissProt format, extracting just the gene name and not the species
        /// Pattern description: match "sp|[protein ID: 6+ alphanumeric]|[CAPTURE gene ID: 2+ alphanumeric]_[species code: 2+ alphanumeric]"
        /// </summary>
        public const string DefaultGeneIdRegexPattern = @"(?<=((sp|tr)\|[0-9A-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})";

        private readonly Regex geneIdRegexTester = new Regex(@"(?<=((sp|tr)\|[0-9A-Z\-]{6,}\|)|^)([A-Z0-9]{2,})(?=_[A-Z0-9]{2,})");

        public ConverterOptions()
        {
            MzidPath = string.Empty;
            TsvPath = string.Empty;
            UnrollResults = false;
            ShowDecoy = false;
            SingleResultPerSpectrum = false;
            IsDirectory = false;
            MaxSpecEValue = 0;
            MaxEValue = 0;
            MaxQValue = 0;
            NoExtendedFields = false;
            AddGeneId = false;

            GeneIdRegexPattern = DefaultGeneIdRegexPattern;
            GeneIdRegex = null;
        }

        [Option("mzid", Required = true, ArgPosition = 1,
            HelpText = "Path to .mzid or .mzid.gz file; if path has spaces, it must be in quotes. " +
                       "Can also provide a directory to convert all .mzid[.gz] files in the directory.")]
        public string MzidPath { get; set; }

        [Option("tsv", ArgPosition = 2,
            HelpText = "Path to tsv file to be written (can be a filename, a directory path, or a file path). " +
                       "If not specified, will be output to the same location as the mzid. " +
                       "If mzid path is a directory, this will be treated as a directory path.")]
        public string TsvPath { get; set; }

        [Option("unroll", "u",
            HelpText = "Unroll the results: output one line per unique peptide/protein combination in each spectrum identification",
            HelpShowsDefault = true)]
        public bool UnrollResults { get; set; }

        [Option("maxSpecEValue", "MaxSpecE", "SpecEValue",
            HelpText = "Maximum SpecEValue filter (ignored if 0 or 1)",
            HelpShowsDefault = true, Min = 0, Max = 1)]
        public double MaxSpecEValue { get; set; }

        [Option("maxEValue", "MaxE", "EValue",
            HelpText = "Maximum EValue filter (ignored if 0)",
            HelpShowsDefault = true, Min = 0, Max = float.MaxValue,
            DefaultValueFormatString = "(Default: {0} Min: {1} Max: {2:0.0E+0})")]
        public double MaxEValue { get; set; }

        [Option("maxQValue", "MaxQ", "QValue",
            HelpText = "Maximum QValue filter (ignored if 0 or 1)",
            HelpShowsDefault = true, Min = 0, Max = 1)]
        public double MaxQValue { get; set; }

        [Option("showDecoy", "sd", HelpText = "Include decoy results in the result tsv", HelpShowsDefault = true)]
        public bool ShowDecoy { get; set; }

        [Option("singleResult", "1", HelpText = "Only output one result per spectrum", HelpShowsDefault = true)]
        public bool SingleResultPerSpectrum { get; set; }

        [Option("skipDupIds",
            HelpText = "If there are issues converting a file due to \"duplicate ID\" errors, " +
                       "specifying this will cause the duplicate IDs to be ignored, " +
                       "at the likely cost of some correctness.", HelpShowsDefault = true)]
        public bool SkipDuplicateIds { get; set; }

        [Option("r", "recurse", HelpText = "If mzid path is a directory, specifying this will cause mzid files in subdirectories to also be converted.")]
        public bool RecurseDirectories { get; set; }

        [Option("ne", "noExtended", HelpText = "If specified, does not add extended fields to the TSV output (e.g., scan time).")]
        public bool NoExtendedFields { get; set; }

        [Option("geneId", "geneName", ArgExistsProperty = nameof(AddGeneId),
            HelpText = "If specified, adds a 'GeneID' column to the output for non-decoy identifications. " +
                       "Optionally supply a regular expression to extract it from the protein identifier and/or protein description. " +
                       "The default expression supports the UniProt SwissProt format.")]
        public string GeneIdRegexPattern { get; set; }

        [Option("geneIdCS", "geneIdCaseSensitive",
            HelpText = "When this is provided, use case-sensitive RegEx matching when looking for gene name")]
        public bool GeneIdRegexIsCaseSensitive { get; set; }

        /// <summary>
        /// This will be auto-set to true if the user specifies -geneId (or if it's defined in a parameter file)
        /// </summary>
        public bool AddGeneId { get; set; }

        /// <summary>
        /// True if we are processing all .mzid or .mzid.gz files in a directory
        /// </summary>
        public bool IsDirectory { get; private set; }

        public List<string> MzidPaths { get; } = new List<string>();

        /// <summary>
        /// RegEx matcher for extracting gene name from protein name and/or protein description
        /// </summary>
        public Regex GeneIdRegex { get; private set; }

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

        public bool ValidateArgs(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(MzidPath))
            {
                errorMessage = "ERROR: mzid path must be specified!";
                return false;
            }

            if (AddGeneId)
            {
                if (string.IsNullOrWhiteSpace(GeneIdRegexPattern))
                {
                    GeneIdRegexPattern = DefaultGeneIdRegexPattern;
                }

                try
                {
                    GeneIdRegex = new Regex(
                        GeneIdRegexPattern,
                        GeneIdRegexIsCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
                catch
                {
                    errorMessage = "ERROR: GeneID Regex is not a valid regular expression: " + GeneIdRegexPattern;
                    return false;
                }
            }

            string mzidFileDirectory;

            if (HasWildcard(MzidPath))
            {
                mzidFileDirectory = GetParentDirectoryPath(MzidPath);
            }
            else
            {
                var mzidFile = new FileInfo(MzidPath);
                if (mzidFile.Exists)
                {
                    mzidFileDirectory = mzidFile.DirectoryName;
                }
                else
                {
                    if (Directory.Exists(MzidPath))
                    {
                        IsDirectory = true;
                        mzidFileDirectory = MzidPath;
                    }
                    else
                    {
                        errorMessage = "ERROR: mzid file does not exist: " + mzidFile.FullName;
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

            if (string.IsNullOrWhiteSpace(TsvPath))
            {
                if (!IsDirectory)
                {
                    TsvPath = AutoNameTsvFromMzid(MzidPath);
                }
            }
            else
            {
                if (HasWildcard(TsvPath))
                {
                    var parentPath = GetParentDirectoryPath(TsvPath);
                    if (!string.IsNullOrWhiteSpace(parentPath))
                        TsvPath = parentPath;
                }

                if (IsDirectory)
                {
                    // Processing all .mzid or .mzid.gz files in a directory

                    // Assure that TsvPath points to a directory, and that it exists
                    if (TsvPath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ||
                        TsvPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        var tsvFile = new FileInfo(TsvPath);
                        TsvPath = string.IsNullOrWhiteSpace(tsvFile.DirectoryName) ? string.Empty : tsvFile.DirectoryName;
                    }

                    var tsvDirectory = new DirectoryInfo(TsvPath);
                    AssureDirectoryExists(tsvDirectory);
                }
                else
                {
                    // Processing a single file, or a set of files specified via a wildcard

                    if (TsvPath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ||
                        TsvPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        // TsvPath has a file name (or path to a file)
                        // Assure that the file's parent directory exists
                        FileInfo tsvFile;
                        if (TsvPath.IndexOf(Path.DirectorySeparatorChar) >= 0 || mzidFileDirectory == null)
                        {
                            tsvFile = new FileInfo(TsvPath);
                        }
                        else
                        {
                            tsvFile = new FileInfo(Path.Combine(mzidFileDirectory, TsvPath));
                            TsvPath = tsvFile.FullName;
                        }

                        AssureDirectoryExists(tsvFile.Directory);
                    }
                    else
                    {
                        // Assure that the directory exists and auto-define the name
                        var tsvDirectory = new DirectoryInfo(TsvPath);
                        AssureDirectoryExists(tsvDirectory);
                        if (HasWildcard(MzidPath))
                        {
                            TsvPath = tsvDirectory.FullName;
                        }
                        else
                        {
                            var tsvFileName = AutoNameTsvFromMzid(Path.GetFileName(MzidPath));
                            TsvPath = Path.Combine(TsvPath, tsvFileName);
                        }
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        private void AssureDirectoryExists(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null || directoryInfo.Exists)
                return;

            ConsoleMsgUtils.ShowWarning("Creating missing directory: " + directoryInfo.FullName);
            directoryInfo.Create();
            Console.WriteLine();
        }

        private string GetParentDirectoryPath(string filePath)
        {
            // Remove all text after the last \ or /
            var lastDirectorySeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            if (lastDirectorySeparator > 0)
            {
                return filePath.Substring(0, lastDirectorySeparator);
            }

            return string.Empty;

        }
        public void OutputSetOptions()
        {
            Console.WriteLine("Using options:");
            if (!IsDirectory)
            {
                Console.WriteLine("mzid path: \"{0}\"", MzidPath);
                Console.WriteLine("tsv path:  \"{0}\"", TsvPath);
            }
            else
            {
                Console.WriteLine("mzid directory: \"{0}\"{1}", MzidPath, RecurseDirectories ? " and subdirectories" : string.Empty);
                if (!string.IsNullOrWhiteSpace(TsvPath))
                {
                    Console.WriteLine("tsv directory:  \"{0}\"", TsvPath);
                }
                Console.WriteLine("mzid and tsv paths:");
                foreach (var path in MzidPaths)
                {
                    Console.WriteLine("\t{0}", path);
                    Console.WriteLine("\t  {0}", AutoNameTsvFromMzid(path));
                }
            }

            Console.WriteLine("Unroll results: {0}", UnrollResults);
            Console.WriteLine("Show decoy: {0}", ShowDecoy);
            Console.WriteLine("Single result per spectrum: {0}", SingleResultPerSpectrum);

            if (AddGeneId)
            {
                Console.WriteLine("Adding gene IDs to the output using regular expression \"{0}\"", GeneIdRegexPattern);
                if (GeneIdRegexIsCaseSensitive)
                {
                    Console.WriteLine("  (case sensitive matching)");
                }
                else
                {
                    Console.WriteLine("  (ignore case when matching)");
                }
            }

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