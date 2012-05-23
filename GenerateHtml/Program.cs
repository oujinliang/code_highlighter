using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateHtml
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("specify input and output file!");
                return;
            }

            string input = args[0];
            string output = args[1];

            HtmlGenerator.Generate(input, output);
        }
    }
}
