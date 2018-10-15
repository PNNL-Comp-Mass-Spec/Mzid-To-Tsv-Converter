using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

                    ConvertToTsv(mzidFile.FullName, tsvPath, options.ShowDecoy, options.UnrollResults, options.SingleResultPerSpectrum, options.SkipDuplicateIds);
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
                    ConvertToTsv(mzidFile, tsvPath, options.ShowDecoy, options.UnrollResults, options.SingleResultPerSpectrum, options.SkipDuplicateIds);
                }
            }
            else
            {
                ConvertToTsv(options.MzidPath, options.TsvPath, options.ShowDecoy, options.UnrollResults, options.SingleResultPerSpectrum, options.SkipDuplicateIds);
            }
        }

        public void ConvertToTsv(string mzidPath, string tsvPath, bool showDecoy = true, bool unrollResults = true, bool singleResult = false, bool skipDuplicateIds = false)
        {
            var reader = new SimpleMZIdentMLReader(skipDuplicateIds, s => Console.WriteLine("MZID PARSE ERROR: {0}", s));
            try
            {
                using (var data = reader.ReadLowMem(mzidPath))
                using (var stream = new StreamWriter(new FileStream(tsvPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    var headers = new List<string>
                    {
                        "#SpecFile",
                        "SpecID",
                        "ScanNum",
                        "FragMethod",
                        "Precursor",
                        "IsotopeError",
                        "PrecursorError(ppm)",
                        "Charge",
                        "Peptide",
                        "Protein",
                        "DeNovoScore",
                        "MSGFScore",
                        "SpecEValue",
                        "EValue",
                        "QValue",
                        "PepQValue"
                    };

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

                    stream.WriteLine(string.Join("\t", headers));

                    var lastScanNum = 0;
                    var resultsWritten = 0;
                    var writtenCount = 0;

                    foreach (var id in data.Identifications)
                    {
                        if (singleResult && id.ScanNum == lastScanNum)
                        {
                            continue;
                        }

                        writtenCount++;

                        lastScanNum = id.ScanNum;
                        var specFile = data.SpectrumFile;
                        var specId = id.NativeId;
                        var scanNum = id.ScanNum;
                        var fragMethod = "CID";
                        if (id.AllParamsDict.ContainsKey("AssumedDissociationMethod"))
                        {
                            fragMethod = id.AllParamsDict["AssumedDissociationMethod"];
                        }

                        var precursor = id.ExperimentalMz;
                        var isotopeError = "0";
                        if (id.AllParamsDict.ContainsKey("IsotopeError"))
                        {
                            isotopeError = id.AllParamsDict["IsotopeError"];
                        }

                        var adjExpMz = id.ExperimentalMz - IsotopeMass * int.Parse(isotopeError) / id.Charge;
                        //var precursorError = (id.CalMz - id.ExperimentalMz) / id.CalMz * 1e6;
                        var precursorError = (adjExpMz - id.CalMz) / id.CalMz * 1e6;

                        var charge = id.Charge;
                        var deNovoScore = id.DeNovoScore;
                        var msgfScore = id.RawScore;
                        var specEValue = id.SpecEv;
                        var eValue = id.EValue;
                        var qValue = id.QValue;
                        var pepQValue = id.PepQValue;

                        var deDup = new HashSet<string>();

                        foreach (var pepEv in id.PepEvidence)
                        {
                            if (!showDecoy && pepEv.IsDecoy)
                            {
                                continue;
                            }

                            var peptideWithModsAndContext = pepEv.SequenceWithNumericMods;
                            // Produce correct output with bad MS-GF+ mzid
                            if (isBadMsGfMzid)
                            {
                                // Add the prefix and suffix residues for this protein
                                // Do not use pepEv.SequenceWithNumericMods; it isn't necessarily correct for this spectrum
                                peptideWithModsAndContext = pepEv.Pre + "." + id.Peptide.SequenceWithNumericMods + "." + pepEv.Post;
                            }

                            var protein = pepEv.DbSeq.Accession;
                            if (!deDup.Add(peptideWithModsAndContext + protein))
                            {
                                continue;
                            }

                            // Write out EValues to 5 sig figs, using scientific notation below 0.0001
                            var specEValueString = StringUtilities.ValueToString(specEValue, 5, 1000);
                            var eValueString = StringUtilities.ValueToString(eValue, 5, 1000);

                            // Write out QValue using 5 digits after the decimal, though use scientific notation below 0.00005
                            var qValueString = StringUtilities.DblToString(qValue, 5, 0.00005);
                            var pepQValueString = StringUtilities.DblToString(pepQValue, 5, 0.00005);

                            if (resultsWritten == 0)
                            {
                                // Assure that the first row has 0.0 for score fields (helps in loading data into Access or SQL server)
                                if (specEValueString == "0") specEValueString = "0.0";
                                if (eValueString == "0") eValueString = "0.0";
                                if (qValueString == "0") qValueString = "0.0";
                                if (pepQValueString == "0") pepQValueString = "0.0";
                            }

                            var line = string.Format(CultureInfo.InvariantCulture,
                                "{0}\t{1}\t{2}\t{3}\t{4:0.0####}\t{5}\t{6:0.0###}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14:0.0####}\t{15:0.0####}",
                                specFile, specId, scanNum, fragMethod, precursor, isotopeError, precursorError, charge, peptideWithModsAndContext,
                                protein,
                                deNovoScore, msgfScore, specEValueString, eValueString, qValueString, pepQValueString);

                            stream.WriteLine(line);

                            resultsWritten += 1;


                            if (!unrollResults)
                            {
                                break;
                            }
                        }
                    }

                    if (writtenCount == 0)
                    {
                        ShowWarning("Warning: .mzID file does not have any results");
                        System.Threading.Thread.Sleep(1500);
                    }
                }
            }
            catch (SimpleMZIdentMLReader.DuplicateKeyException ex)
            {
                ConsoleMsgUtils.ShowError(string.Format("MZID PARSE ERROR: {0}", ex.Message), ex);
                ConsoleMsgUtils.ShowWarning("This type of error is usually caused by an error in the MZID output.");
            }
        }

        public const double C = 12.0f;
        public const double C13 = 13.00335483;
        public const double IsotopeMass = C13 - C;

        private void ShowWarning(string warningMessage)
        {
            ConsoleMsgUtils.ShowWarning(warningMessage);
        }
    }
}
