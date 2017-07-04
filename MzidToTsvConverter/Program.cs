using System;
using System.Linq;

namespace MzidToTsvConverter
{
    public static class Program
    {
        static int Main(string[] args)
        {
            //var optionsParser = CommandLine.Parser.Default.ParseArguments<ConverterOptions>(args);
            //if (optionsParser.Errors.Any())
            //{
            //    //Console.WriteLine(optionsParser.Errors);
            //    return;
            //}
            //var options = optionsParser.Value;
            var options = new ConverterOptions();
            if (!options.ProcessArgs(args))
            {
                System.Threading.Thread.Sleep(1500);
                return -1;
            }

            options.OutputSetOptions();

#if !DEBUG
            try
            {
#endif
                var converter = new MzidToTsvConverter();
                converter.ConvertToTsv(options);
                Console.WriteLine("Conversion finished!");
                System.Threading.Thread.Sleep(700);
                return 0;
#if !DEBUG
        }
            catch (Exception e)
            {
                Console.WriteLine("Conversion failed: " + e.Message);
                Console.WriteLine(e.StackTrace);

                System.Threading.Thread.Sleep(1500);
                var errorCode = e.Message.GetHashCode();
                if (errorCode == 0)
                    return -1;
                return errorCode;
            }
#endif
        }
    }
}
