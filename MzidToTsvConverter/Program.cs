using System;

namespace MzidToTsvConverter
{
    public class Program
    {
        static void Main(string[] args)
        {
            var options = new ConverterOptions();
            if (!options.ProcessArgs(args))
            {
                return;
            }

            options.OutputSetOptions();

            try
            {
                MzidToTsvConverter.ConvertToTsv(options);
                System.Console.WriteLine("Conversion finished!");
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Conversion failed: " + e.Message);
            }
        }
    }
}
