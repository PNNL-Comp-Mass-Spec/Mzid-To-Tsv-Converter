using System;
using System.Collections.Generic;
using System.IO;
using CsvHelper;
using PRISM;
using PSI_Interface.IdentData;

namespace MzidToTsvConverter
{
    public class MzidToTsvConverter
    {
        public void ConvertToTsv(ConverterOptions options)
        {
            if (options.HasWildcard(options.MzidPath))
            {
                // Find matching files
                var mzidFiles = PathUtils.FindFilesWildcard(options.MzidPath);

                if (mzidFiles.Count == 0)
                {
                    ShowWarning("No mzid files were found with path spec " + options.MzidPath);
                    return;
                }

                foreach (var mzidFile in mzidFiles)
                {
                    Console.WriteLine();
                    Console.WriteLine("Converting " + mzidFile.FullName);

                    string tsvPath;
                    if (options.HasWildcard(options.TsvPath))
                    {
                        tsvPath = options.AutoNameTsvFromMzid(mzidFile.FullName);
                    }
                    else
                    {
                        tsvPath = options.TsvPath;
                    }

                    ConvertToTsv(mzidFile.FullName, tsvPath, options);
                }
            }
            else if (options.IsDirectory)
            {
                if (options.MzidPaths.Count == 0)
                {
                    var subDirsMessage = options.RecurseDirectories ? " or subdirectories" : "";
                    ShowWarning($"No mzid[.gz] files found in directory \"{options.MzidPath}\"{subDirsMessage}.");
                    return;
                }

                foreach (var mzidFile in options.MzidPaths)
                {
                    var tsvPath = options.AutoNameTsvFromMzid(mzidFile);
                    ConvertToTsv(mzidFile, tsvPath, options);
                }
            }
            else
            {
                ConvertToTsv(options.MzidPath, options.TsvPath, options);
            }
        }

        public void ConvertToTsv(
            string mzidPath,
            string tsvPath,
            ConverterOptions options)
        {
            var filterOnSpecEValue = options.FilterEnabled(options.MaxSpecEValue);
            var filterOnEValue = options.MaxEValue > 0;
            var filterOnQValue = options.FilterEnabled(options.MaxQValue);

            var tsvFile = new FileInfo(tsvPath);
            if (tsvFile.Exists)
            {
                ShowWarning("Overwriting existing file: " + tsvFile.Name);
                Console.WriteLine();
            }

            var reader = new SimpleMZIdentMLReader(options.SkipDuplicateIds, s => Console.WriteLine("MZID PARSE ERROR: {0}", s));
            try
            {
                using (var data = reader.ReadLowMem(mzidPath))
                using (var csv = new CsvWriter(new StreamWriter(new FileStream(tsvFile.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))))
                {
                    csv.Configuration.AllowComments = false;
                    csv.Configuration.Delimiter = "\t";
                    csv.Configuration.RegisterClassMap(new PeptideMatchMap());

                    // SPECIAL CASE:
                    // Certain versions of MS-GF+ output incorrect mzid files - the peptides referenced in the peptide_ref attribute in
                    // SpectrumIdentificationItems was correct, but if there was a modification in the first 3 residues there was at
                    // least a 50% chance of the PeptideEvidenceRefs within the SpectrumIdentificationItem being incorrect. So, for
                    // those bad versions, use the peptide_ref rather than the PeptideEvidenceRefs to get the sequence.
                    var isBadMsGfMzid = false;
                    if (data.AnalysisSoftwareCvAccession.ToUpper().Contains("MS:1002048") && !string.IsNullOrWhiteSpace(data.AnalysisSoftwareVersion))
                    {
                        // bad versions: v10280 (introduced), v10282, v2016.01.20, v2016.01.21, v2016.01.29, v2016.02.12, v2016.05.25, v2016.0.13, v2016.06.13, v2016.06.14, v2016.06.15, v2016.06.29, v2016.07.26, v2016.08.31, v2016.09.07, v2016.09.22, v2016.09.23 (fixed with version v2016.10.10)
                        var badVersions = new[]
                        {
                            "v10280", "v10282", "v2016.01.20", "v2016.01.21", "v2016.01.29", "v2016.02.12", "v2016.05.25", "v2016.0.13",
                            "v2016.06.13",
                            "v2016.06.14", "v2016.06.15", "v2016.06.29", "v2016.07.26", "v2016.08.31", "v2016.09.07", "v2016.09.22", "v2016.09.23"
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
                        ShowWarning(string.Format(
                            "Warning: file \"{0}\" was created with a version of MS-GF+ that had some erroneous output in the mzid file." +
                            " Using sequences from the peptide_ref attribute instead of the PeptideEvidenceRef element to try to bypass the issue.",
                            mzidPath));
                    }

                    csv.WriteHeader<PeptideMatch>();
                    csv.NextRecord();

                    var lastScanNum = 0;

                    var writtenCount = 0;

                    // Number of items in data.Identifications
                    // Incremented during the foreach loop
                    var unfilteredCount = 0;

                    // Number of identifications that did not pass the score filters
                    var filteredOutCount = 0;

                    foreach (var id in data.Identifications)
                    {
                        if (options.SingleResultPerSpectrum && id.ScanNum == lastScanNum)
                        {
                            continue;
                        }

                        unfilteredCount++;

                        lastScanNum = id.ScanNum;
                        var match = new PeptideMatch()
                        {
                            SpecFile = data.SpectrumFile,
                            Identification = id,
                        };

                        if (filterOnSpecEValue && match.SpecEValue > options.MaxSpecEValue)
                        {
                            filteredOutCount++;
                            continue;
                        }

                        if (filterOnEValue && match.EValue > options.MaxEValue)
                        {
                            filteredOutCount++;
                            continue;
                        }

                        if (filterOnQValue && match.QValue > options.MaxQValue)
                        {
                            filteredOutCount++;
                            continue;
                        }

                        var uniquePepProteinList = new HashSet<string>();

                        var resultWritten = false;

                        foreach (var pepEv in id.PepEvidence)
                        {
                            if (!options.ShowDecoy && pepEv.IsDecoy)
                            {
                                continue;
                            }

                            match.Peptide = pepEv.SequenceWithNumericMods;

                            // Produce correct output with bad MS-GF+ mzid
                            if (isBadMsGfMzid)
                            {
                                // Add the prefix and suffix residues for this protein
                                // Do not use pepEv.SequenceWithNumericMods; it isn't necessarily correct for this spectrum
                                match.Peptide = pepEv.Pre + "." + id.Peptide.SequenceWithNumericMods + "." + pepEv.Post;
                            }

                            match.Protein = pepEv.DbSeq.Accession;
                            if (!uniquePepProteinList.Add(match.Peptide + match.Protein))
                            {
                                continue;
                            }

                            csv.WriteRecord(match);
                            csv.NextRecord();

                            resultWritten = true;

                            if (!options.UnrollResults)
                            {
                                break;
                            }
                        }

                        if (resultWritten)
                            writtenCount++;
                    }

                    if (unfilteredCount == 0)
                    {
                        ShowWarning("Warning: .mzID file does not have any results");
                        System.Threading.Thread.Sleep(1500);
                    }
                    else if (writtenCount == 0)
                    {
                        ShowWarning("Warning: none of the results passed the specified filter(s)");
                        System.Threading.Thread.Sleep(1500);
                    }
                    else
                    {
                        Console.WriteLine("Wrote {0:N0} results to {1}", writtenCount, PathUtils.CompactPathString(tsvFile.FullName, 70));
                        if (filteredOutCount > 0)
                        {
                            Console.WriteLine("Filtered out {0:N0} results", filteredOutCount);
                        }
                    }
                }
            }
            catch (SimpleMZIdentMLReader.DuplicateKeyException ex)
            {
                ConsoleMsgUtils.ShowError(string.Format("MZID PARSE ERROR: {0}", ex.Message), ex);
                ConsoleMsgUtils.ShowWarning("This type of error is usually caused by an error in the MZID output.");
            }
        }

        private void ShowWarning(string warningMessage)
        {
            ConsoleMsgUtils.ShowWarning(warningMessage);
        }
    }
}
