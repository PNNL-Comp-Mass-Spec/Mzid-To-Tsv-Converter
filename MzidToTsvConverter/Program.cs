using System;
using System.IO;
using System.Reflection;
using System.Threading;
using PRISM;

namespace MzidToTsvConverter
{
    public static class Program
    {
        // Ignore Spelling: Bryson, Conf

        private static int Main(string[] args)
        {
            var asmName = typeof(Program).GetTypeInfo().Assembly.GetName();
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);       // Alternatively: System.AppDomain.CurrentDomain.FriendlyName
            var programVersion = typeof(Program).GetTypeInfo().Assembly.GetName().Version;
            var version = $"version {programVersion.Major}.{programVersion.Minor}.{programVersion.Build}";

            var parser = new CommandLineParser<ConverterOptions>(asmName.Name, version)
            {
                ProgramInfo = "This program converts a .mzid file created by MS-GF+ into a tab-delimited text file.",

                ContactInfo = "Program written by Bryson Gibbons for the Department of Energy " + Environment.NewLine +
                              "(PNNL, Richland, WA) in 2018" + Environment.NewLine + Environment.NewLine +
                              string.Format(
                                  "Version: {0}.{1}.{2} ({3})",
                                  programVersion.Major, programVersion.Minor, programVersion.Build, ConverterOptions.PROGRAM_DATE) +
                              Environment.NewLine + Environment.NewLine +
                              "E-mail: bryson.gibbons@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine +
                              "Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics",

                UsageExamples = {
                    exeName + " Results.mzid",
                    exeName + " Results.mzid -unroll",
                    exeName + " Results.mzid -unroll -showDecoy",
                }
            };

            parser.AddParamFileKey("Conf");

            var parseResults = parser.ParseArgs(args);
            var options = parseResults.ParsedResults;

            if (!parseResults.Success)
            {
                Thread.Sleep(1500);
                return -1;
            }

            if (!options.ValidateArgs(out var errorMessage))
            {
                parser.PrintHelp();

                Console.WriteLine();
                ConsoleMsgUtils.ShowWarning("Validation error:");
                ConsoleMsgUtils.ShowWarning(errorMessage);

                Thread.Sleep(1500);
                return -1;
            }

            options.OutputSetOptions();

            try
            {
                var converter = new MzidToTsvConverter();
                converter.ConvertToTsv(options);

                Console.WriteLine();
                Console.WriteLine("Conversion finished!");
                Thread.Sleep(700);

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Conversion failed", ex);

                Thread.Sleep(1500);
                var errorCode = ex.Message.GetHashCode();
                if (errorCode == 0)
                    return -1;
                return errorCode;
            }
        }
    }
}
