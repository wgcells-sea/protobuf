using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Attribute;

namespace CommandLineParser.UnitTests
{
    class TestOptions
    {
        [Option("i", null, Required = true, HelpText = "Input file to read.")]
        public string InputFile { get; set; }

        [Option("o", "output", Required = true, HelpText = "Input file to read.")]
        public string OutputFile { get; set; }

        [Option("l", "length", HelpText = "The maximum number of bytes to process.")]
        public int MaximumLength { get; set; }

        [Option("v", "verbose", HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }
    }

    class TestIndexedOptions
    {
        [Option("i", Index = 0, Required = true, HelpText = "Input file to read.")]
        public string InputFile { get; set; }

        [Option("o", "output", Index = 1, Required = true, HelpText = "Input file to read.")]
        public string OutputFile { get; set; }

        [Option("l", "length", HelpText = "The maximum number of bytes to process.")]
        public int MaximumLength { get; set; }

        [Option("v", "verbose", HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }
    }
}
