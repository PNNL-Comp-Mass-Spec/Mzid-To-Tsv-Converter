using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CsvHelper;
using CsvHelper.Configuration;
using PRISM;
using PSI_Interface.IdentData;

namespace MzidToTsvConverter
{
    public class MzidToTsvConverter
    {
        // Ignore Spelling: mzid, tsv

        /// <summary>
        /// Convert the .mzid file(s) specified by options.MzidPath
        /// </summary>
        /// <param name="options">Processing options</param>
        /// <returns>True if successful for at least one .mzid file, false if an error</returns>
        public bool ConvertToTsv(ConverterOptions options)
        {
            var fileCountConverted = 0;

            if (ConverterOptions.HasWildcard(options.MzidPath))
            {
                // Find matching files
                var mzidFiles = PathUtils.FindFilesWildcard(options.MzidPath);

                if (mzidFiles.Count == 0)
                {
                    ConsoleMsgUtils.ShowWarning("No mzid files were found with path spec " + options.MzidPath);
                    return false;
                }

                foreach (var mzidFile in mzidFiles)
                {
                    Console.WriteLine();
                    Console.WriteLine("Converting " + mzidFile.FullName);

                    string tsvPath;
                    if (ConverterOptions.HasWildcard(options.TsvPath))
                    {
                        tsvPath = options.AutoNameTsvFromMzid(mzidFile.FullName);
                    }
                    else
                    {
                        if (Directory.Exists(options.TsvPath))
                        {
                            tsvPath = Path.Combine(options.TsvPath, options.AutoNameTsvFromMzid(mzidFile.Name));
                        }
                        else
                        {
                            tsvPath = options.TsvPath;
                        }
                    }

                    var success = ConvertToTsv(mzidFile.FullName, tsvPath, options);
                    if (success)
                        fileCountConverted++;
                }
                return fileCountConverted > 0;
            }

            if (options.IsDirectory)
            {
                if (options.MzidPaths.Count == 0)
                {
                    var subDirsMessage = options.RecurseDirectories ? " or subdirectories" : string.Empty;
                    ConsoleMsgUtils.ShowWarning($"No mzid[.gz] files found in directory \"{options.MzidPath}\"{subDirsMessage}.");
                    return false;
                }

                foreach (var mzidFile in options.MzidPaths)
                {
                    var tsvPath = options.AutoNameTsvFromMzid(mzidFile);
                    var success = ConvertToTsv(mzidFile, tsvPath, options);
                    if (success)
                        fileCountConverted++;
                }
                return fileCountConverted > 0;
            }

            return ConvertToTsv(options.MzidPath, options.TsvPath, options);
        }

        /// <summary>
        /// Convert the given .mzid file to a .tsv file
        /// </summary>
        /// <param name="mzidPath">.mzid file to read (supports .mzid.gz)</param>
        /// <param name="tsvPath">.tsv file to create (cannot be an empty string)</param>
        /// <param name="options">Processing options</param>
        /// <returns>True if successful, false if an error</returns>
        public bool ConvertToTsv(
            string mzidPath,
            string tsvPath,
            ConverterOptions options)
        {
            var filterOnSpecEValue = ConverterOptions.FilterEnabled(options.MaxSpecEValue);
            var filterOnEValue = options.MaxEValue > 0;
            var filterOnQValue = ConverterOptions.FilterEnabled(options.MaxQValue);

            if (string.IsNullOrWhiteSpace(tsvPath))
            {
                ConsoleMsgUtils.ShowWarning("The target .tsv file path must be defined when calling ConvertToTsv with file paths");
                Thread.Sleep(1500);
                return false;
            }

            var tsvFile = new FileInfo(tsvPath);

            if (tsvFile.Exists)
            {
                ConsoleMsgUtils.ShowWarning("Overwriting existing file: " + PathUtils.CompactPathString(tsvFile.FullName, 90));
                Console.WriteLine();
            }
            else
            {
                ConsoleMsgUtils.ShowWarning("Creating: " + PathUtils.CompactPathString(tsvFile.FullName, 115));
            }

            var writtenCount = 0;

            // DelimitedProteinNames takes precedence over UnrollResults
            // However, behavior below needs to be the same for UnrollResults and DelimitedProteinNames
            var maxMatchedProteins = 1;
            if (options.UnrollResults || options.DelimitedProteinNames)
            {
                maxMatchedProteins = int.MaxValue;
            }

            var reader = new SimpleMZIdentMLReader(options.SkipDuplicateIds, s => Console.WriteLine("MZID PARSE ERROR: {0}", s));
            try
            {
                var configuration = new CsvConfiguration(CultureInfo.CurrentCulture)
                {
                    AllowComments = false,
                    Delimiter = "\t"
                };

                using var data = reader.ReadLowMem(mzidPath);
                using var writer = new StreamWriter(new FileStream(tsvFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                using var csv = new CsvWriter(writer, configuration);

                csv.Context.RegisterClassMap(new PeptideMatchMap(options.NoExtendedFields, options.AddGeneId));

                // SPECIAL CASE:
                // Certain versions of MS-GF+ output incorrect mzid files - the peptides referenced in the peptide_ref attribute in
                // SpectrumIdentificationItems was correct, but if there was a modification in the first 3 residues there was at
                // least a 50% chance of the PeptideEvidenceRefs within the SpectrumIdentificationItem being incorrect. So, for
                // those bad versions, use the peptide_ref rather than the PeptideEvidenceRefs to get the sequence.
                var isBadMsGfMzid = false;

                if (data.AnalysisSoftwareCvAccession.IndexOf("MS:1002048", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    !string.IsNullOrWhiteSpace(data.AnalysisSoftwareVersion))
                {
                    // bad versions: v10280 (introduced), v10282, v2016.01.20, v2016.01.21, v2016.01.29, v2016.02.12, v2016.05.25, v2016.0.13, v2016.06.13, v2016.06.14, v2016.06.15, v2016.06.29, v2016.07.26, v2016.08.31, v2016.09.07, v2016.09.22, v2016.09.23 (fixed with version v2016.10.10)
                    var badVersions = new[]
                    {
                        "v10280", "v10282",
                        "v2016.01.20", "v2016.01.21", "v2016.01.29", "v2016.02.12", "v2016.05.25", "v2016.0.13",
                        "v2016.06.13", "v2016.06.14", "v2016.06.15", "v2016.06.29", "v2016.07.26",
                        "v2016.08.31", "v2016.09.07", "v2016.09.22", "v2016.09.23"
                    };

                    foreach (var version in badVersions)
                    {
                        if (data.AnalysisSoftwareVersion.Contains(version))
                        {
                            isBadMsGfMzid = true;
                        }
                    }
                }

                if (isBadMsGfMzid)
                {
                    ConsoleMsgUtils.ShowWarning(
                        "Warning: file \"{0}\" was created with a version of MS-GF+ that had some erroneous output in the mzid file." +
                        " Using sequences from the peptide_ref attribute instead of the PeptideEvidenceRef element to try to bypass the issue.",
                        mzidPath);
                }

                csv.WriteHeader<PeptideMatch>();
                csv.NextRecord();

                var lastScanNum = 0;

                // Number of items in data.Identifications
                // Incremented during the for each loop
                var unfilteredCount = 0;

                // Number of identifications that did not pass the score filters
                var filteredOutCount = 0;

                // List of matches in a single result. List is cleared before use.
                // Only contains multiple when outputting all protein matches, and a result has multiple protein matches.
                var matches = new List<PeptideMatch>(30);
                foreach (var id in data.Identifications)
                {
                    if (options.SingleResultPerSpectrum && id.ScanNum == lastScanNum)
                    {
                        continue;
                    }

                    unfilteredCount++;

                    lastScanNum = id.ScanNum;

                    if (filterOnSpecEValue && id.SpecEv > options.MaxSpecEValue)
                    {
                        filteredOutCount++;
                        continue;
                    }

                    if (filterOnEValue && id.EValue > options.MaxEValue)
                    {
                        filteredOutCount++;
                        continue;
                    }

                    if (filterOnQValue && id.QValue > options.MaxQValue)
                    {
                        filteredOutCount++;
                        continue;
                    }

                    // Clear out the list of matches.
                    matches.Clear();
                    var uniquePepProteinList = new HashSet<string>();

                    // id.PepEvidence has one entry for each protein associated with this PSM
                    IEnumerable<SimpleMZIdentMLReader.PeptideEvidence> pepEvEnum = id.PepEvidence;
                    if (!options.ShowDecoy)
                    {
                        pepEvEnum = pepEvEnum.Where(x => !x.IsDecoy);
                    }

                    // maxMatchedProteins is '1' or 'int.MaxValue'
                    foreach (var pepEv in pepEvEnum.Take(maxMatchedProteins))
                    {
                        var peptide = pepEv.SequenceWithNumericMods;

                        // Produce correct output with bad MS-GF+ mzid
                        if (isBadMsGfMzid)
                        {
                            // Add the prefix and suffix residues for this protein
                            // Do not use pepEv.SequenceWithNumericMods; it isn't necessarily correct for this spectrum
                            peptide = pepEv.Pre + "." + id.Peptide.SequenceWithNumericMods + "." + pepEv.Post;
                        }

                        var protein = pepEv.DbSeq.Accession;

                        if (!uniquePepProteinList.Add(peptide + protein))
                        {
                            // Don't process the check for the gene ID if it's not a unique match
                            continue;
                        }

                        var geneId = string.Empty;
                        if (options.AddGeneId && !pepEv.IsDecoy)
                        {
                            // Note that .ProteinDescription includes both the Protein Name and the Description
                            var success = TryGetGeneId(options.GeneIdRegex, pepEv.DbSeq.ProteinDescription, out geneId);
                            if (!success)
                            {
                                geneId = string.Empty;
                            }
                        }

                        matches.Add(new PeptideMatch
                        {
                            SpecFile = data.SpectrumFile,
                            Identification = id,
                            Peptide = peptide,
                            Protein = protein,
                            GeneId = geneId,
                        });
                    }

                    if (matches.Count == 0)
                        continue;

                    if (options.DelimitedProteinNames && matches.Count > 1)
                    {
                        CombineProteinNames(options, matches);

                        // The first item in matches already lists all of the protein names; remove all remaining matches.
                        matches.RemoveRange(1, matches.Count - 1);
                    }

                    foreach (var item in matches)
                    {
                        csv.WriteRecord(item);
                        csv.NextRecord();
                    }

                    writtenCount++;
                }

                if (unfilteredCount == 0)
                {
                    ConsoleMsgUtils.ShowWarning("Warning: .mzid file does not have any results");
                    Thread.Sleep(1500);
                }
                else if (writtenCount == 0)
                {
                    ConsoleMsgUtils.ShowWarning("Warning: none of the results passed the specified filter(s)");
                    Thread.Sleep(1500);
                }
                else
                {
                    Console.WriteLine("Wrote {0:N0} results to {1}", writtenCount, PathUtils.CompactPathString(tsvFile.FullName, 70));
                    if (filteredOutCount > 0)
                    {
                        Console.WriteLine("Filtered out {0:N0} results", filteredOutCount);
                    }
                }

                return true;
            }
            catch (SimpleMZIdentMLReader.DuplicateKeyException ex)
            {
                ConsoleMsgUtils.ShowError("MZID PARSE ERROR", ex);
                ConsoleMsgUtils.ShowWarning("This type of error is usually caused by an error in the MZID output.");
                return false;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError(
                    string.Format("Error converting the file (so far, {0:N0} results have been written", writtenCount), ex);
                return false;
            }
        }

        public static bool TryGetGeneId(Regex geneIdRegex, string proteinNameAndDescription, out string geneId)
        {
            var geneMatch = geneIdRegex.Match(proteinNameAndDescription);
            if (!geneMatch.Success)
            {
                geneId = string.Empty;
                return false;
            }

            // This handles RegEx with patterns like "GN=([^\s|]+)", returning the capture group instead of the entire match
            // It also handles several other formats; the only issue is if there are any capture groups that are not non-capture groups.
            if (geneMatch.Groups.Count > 1)
            {
                geneId = geneMatch.Groups[geneMatch.Groups.Count - 1].Value;
                return true;
            }

            // If there are captures, use the first one
            if (geneMatch.Captures.Count > 0)
            {
                geneId = geneMatch.Captures[0].Value;
                return true;
            }

            geneId = geneMatch.Value;
            return true;
        }

        private static void CombineProteinNames(ConverterOptions options, IReadOnlyList<PeptideMatch> matches)
        {
            const int MAX_LIST_LENGTH = 100000;

            var proteinNames = new SortedSet<string>();
            var proteinNameLength = 0;

            var proteinDelimiterLength = options.ProteinNameDelimiter.Length;

            foreach (var item in matches)
            {
                if (!proteinNames.Contains(item.Protein))
                {
                    proteinNames.Add(item.Protein);
                    proteinNameLength += item.Protein.Length + proteinDelimiterLength;
                }

                if (proteinNameLength > MAX_LIST_LENGTH)
                {
                    break;
                }
            }

            // Store the list of protein names in the first item in matches
            matches[0].Protein = string.Join(options.ProteinNameDelimiter, proteinNames);

            if (proteinNameLength > MAX_LIST_LENGTH)
            {
                matches[0].Protein = string.Format("{0}{1}{2}", matches[0].Protein, options.ProteinNameDelimiter, "...");
            }
        }
    }
}
