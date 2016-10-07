using System;

namespace MzidToTsvConverter
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var options = new ConverterOptions();
            if (!options.ProcessArgs(args))
            {
                System.Threading.Thread.Sleep(1500);
                return;
            }

            options.OutputSetOptions();

            try
            {
                var converter = new MzidToTsvConverter();
                converter.ConvertToTsv(options);
                Console.WriteLine("Conversion finished!");
                System.Threading.Thread.Sleep(700);
            }
            catch (Exception e)
            {
                Console.WriteLine("Conversion failed: " + e.Message);
                System.Threading.Thread.Sleep(1500);
            }
        }
    }
}
