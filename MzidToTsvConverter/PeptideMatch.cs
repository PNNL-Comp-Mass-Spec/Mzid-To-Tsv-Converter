using CsvHelper.Configuration;
using PRISM;
using PSI_Interface.IdentData;

namespace MzidToTsvConverter
{
    public class PeptideMatch
    {
        public const double C = 12.0f;
        public const double C13 = 13.00335483;
        public const double IsotopeMass = C13 - C;

        public string SpecFile { get; set; }
        public SimpleMZIdentMLReader.SpectrumIdItem Identification { get; set; }
        public string SpecId => Identification.NativeId;
        public int ScanNum => Identification.ScanNum;
        public double ScanStartTimeMinutes => Identification.ScanTimeMinutes;

        public string FragMethod
        {
            get
            {
                if (Identification.AllParamsDict.TryGetValue("AssumedDissociationMethod", out var fragMethod))
                {
                    return fragMethod;
                }
                return "CID";
            }
        }

        public double Precursor => Identification.ExperimentalMz;
        public int IsotopeError => Identification.IsoError;

        public double PrecursorErrorPpm
        {
            get
            {
                var adjExpMz = Identification.ExperimentalMz - IsotopeMass * Identification.IsoError / Identification.Charge;
                return (adjExpMz - Identification.CalMz) / Identification.CalMz * 1e6;
            }
        }

        public int Charge => Identification.Charge;
        public string Peptide { get; set; }
        public string Protein { get; set; }
        public string GeneId { get; set; }
        public int DeNovoScore => Identification.DeNovoScore;
        public double MSGFScore => Identification.RawScore;
        public double SpecEValue => Identification.SpecEv;
        public double EValue => Identification.EValue;
        public double QValue => Identification.QValue;
        public double PepQValue => Identification.PepQValue;
    }

    public class PeptideMatchMap : ClassMap<PeptideMatch>
    {
        public PeptideMatchMap(bool noExtendedFields = false, bool addGeneId = false)
        {
            var index = 0;
            Map(x => x.SpecFile).Name("#SpecFile", "SpecFile").Index(index++);
            Map(x => x.SpecId).Name("SpecId").Index(index++);
            Map(x => x.ScanNum).Name("ScanNum").Index(index++);
            if (!noExtendedFields)
            {
                Map(x => x.ScanStartTimeMinutes).Name("ScanTime(Min)").Index(index++).TypeConverterOption.Format("0.0####");
            }
            Map(x => x.FragMethod).Name("FragMethod").Index(index++);
            Map(x => x.Precursor).Name("Precursor").Index(index++).TypeConverterOption.Format("0.0####");
            Map(x => x.IsotopeError).Name("IsotopeError").Index(index++);
            Map(x => x.PrecursorErrorPpm).Name("PrecursorError(ppm)").Index(index++).TypeConverterOption.Format("0.0####");
            Map(x => x.Charge).Name("Charge").Index(index++);
            Map(x => x.Peptide).Name("Peptide").Index(index++);
            Map(x => x.Protein).Name("Protein").Index(index++);
            if (addGeneId)
            {
                Map(x => x.GeneId).Name("GeneID").Index(index++);
            }
            Map(x => x.DeNovoScore).Name("DeNovoScore").Index(index++);
            Map(x => x.MSGFScore).Name("MSGFScore").Index(index++);

            // Write out EValues to 5 sig figs, using scientific notation below 0.0001
            Map(x => x.SpecEValue).Name("SpecEValue").Index(index++).ConvertUsing(x => ShowDecimalForZero(StringUtilities.ValueToString(x.SpecEValue, 5, 1000)));
            Map(x => x.EValue).Name("EValue").Index(index++).ConvertUsing(x => ShowDecimalForZero(StringUtilities.ValueToString(x.EValue, 5, 1000)));

            // Write out QValue using 5 digits after the decimal, though use scientific notation below 0.00005
            Map(x => x.QValue).Name("QValue").Index(index++).ConvertUsing(x => ShowDecimalForZero(StringUtilities.ValueToString(x.QValue, 5, 0.00005)));

            // ReSharper disable once RedundantAssignment
            Map(x => x.PepQValue).Name("PepQValue").Index(index++).ConvertUsing(x => ShowDecimalForZero(StringUtilities.ValueToString(x.PepQValue, 5, 0.00005)));
        }

        private static string ShowDecimalForZero(string input)
        {
            // Assure that the first row has 0.0 for score fields (helps in loading data into Access or SQL server)
            if (input.Equals("0"))
            {
                return "0.0";
            }

            return input;
        }
    }
}
