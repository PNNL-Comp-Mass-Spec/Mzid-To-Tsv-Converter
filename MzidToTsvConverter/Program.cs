using System;

namespace MzidToTsvConverter
{
    public static class Program
    {
        static int Main(string[] args)
        {
            var options = new ConverterOptions();
            if (!options.ProcessArgs(args))
            {
                System.Threading.Thread.Sleep(1500);
                return -1;
            }

            options.OutputSetOptions();

            try
            {
                var converter = new MzidToTsvConverter();
                converter.ConvertToTsv(options);
                Console.WriteLine("Conversion finished!");
                System.Threading.Thread.Sleep(700);
                return 0;
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
        }
    }
}
