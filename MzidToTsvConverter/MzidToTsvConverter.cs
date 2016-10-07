using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PNNLOmics.Utilities;
using PSI_Interface.IdentData;

namespace MzidToTsvConverter
{
    public class MzidToTsvConverter
    {
        public static void ConvertToTsv(ConverterOptions options)
        {
            ConvertToTsv(options.MzidPath, options.TsvPath, options.ShowDecoy, options.UnrollResults);
        }

        public static void ConvertToTsv(string mzidPath, string tsvPath, bool showDecoy = true, bool unrollResults = true)
        {
            var reader = new SimpleMZIdentMLReader();
            var data = reader.Read(mzidPath);

            var headers = "#SpecFile\tSpecID\tScanNum\tFragMethod\tPrecursor\tIsotopeError\tPrecursorError(ppm)\tCharge\tPeptide\tProtein\tDeNovoScore\tMSGFScore\tSpecEValue\tEValue\tQValue\tPepQValue";
            //var format = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}";

            using (var stream = new StreamWriter(new FileStream(tsvPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                stream.WriteLine(headers);
                foreach (var id in data.Identifications)
                {
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

                    var dedup = new HashSet<string>();
                    foreach (var pepEv in id.PepEvidence)
                    {
                        if (!showDecoy && pepEv.IsDecoy)
                        {
                            continue;
                        }
                        var peptide = pepEv.SequenceWithNumericMods;
                        var protein = pepEv.DbSeq.Accession;
                        if (!dedup.Add(peptide + protein))
                        {
                            continue;
                        }
                        /*var line = string.Format(CultureInfo.InvariantCulture,
                            "{0}\t{1}\t{2}\t{3}\t{4:0.0####}\t{5}\t{6:0.0###}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:G6}\t{13:G6}\t{14:0.0####}\t{15:0.0####}",
                            specFile, specId,
                            scanNum, fragMethod, precursor, isotopeError, precursorError, charge, peptide, protein, deNovoScore, msgfScore, specEValue,
                            eValue, qValue, pepQValue);
                        stream.WriteLine(line);*/
                        /*stream.WriteLine(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}\t{4:0.0####}\t{5}\t{6:0.0###}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12:0.0####}\t{13:0.0####}\t{14:0.0####}\t{15:0.0####}", specFile, specId,
                            scanNum, fragMethod, precursor, isotopeError, precursorError, charge, peptide, protein, deNovoScore, msgfScore, specEValue,
                            eValue, qValue, pepQValue);*/
                        var specEValueString = StringUtilities.DblToString(specEValue, 5, true, 0.001);
                        var eValueString = StringUtilities.DblToString(eValue, 5, true, 0.001);
                        var qValueString = StringUtilities.DblToString(qValue, 5);
                        var pepQValueString = StringUtilities.DblToString(pepQValue, 5);
                        if (!qValueString.Contains(".") && !qValueString.ToLower().Contains("e"))
                        {
                            qValueString = qValueString + ".0";
                        }
                        if (!pepQValueString.Contains(".") && !pepQValueString.ToLower().Contains("e"))
                        {
                            pepQValueString = pepQValueString + ".0";
                        }
                        var line = string.Format(CultureInfo.InvariantCulture,
                            "{0}\t{1}\t{2}\t{3}\t{4:0.0####}\t{5}\t{6:0.0###}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}",
                            specFile, specId,
                            scanNum, fragMethod, precursor, isotopeError, precursorError, charge, peptide, protein, deNovoScore, msgfScore, specEValueString,
                            eValueString, qValueString, pepQValueString);
                        stream.WriteLine(line);

                        if (!unrollResults)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public const double C = 12.0f;
        public const double C13 = 13.00335483;
        public const double IsotopeMass = C13 - C;
    }
}
