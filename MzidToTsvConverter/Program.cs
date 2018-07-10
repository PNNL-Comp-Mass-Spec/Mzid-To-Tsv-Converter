using System;
using System.Reflection;
using PRISM;

namespace MzidToTsvConverter
{
    public static class Program
    {
        static int Main(string[] args)
        {
            var options = new ConverterOptions();
            var asmName = typeof(Program).GetTypeInfo().Assembly.GetName();
            var programVersion = typeof(Program).GetTypeInfo().Assembly.GetName().Version;
            var version = $"version {programVersion.Major}.{programVersion.Minor}.{programVersion.Build}";
            if (!CommandLineParser<ConverterOptions>.ParseArgs(args, options, asmName.Name, version) || !options.ValidateArgs())
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
