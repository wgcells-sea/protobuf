using System;

namespace CommandLineParser.Attribute
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : System.Attribute
    {
        public string ShortName { get; private set; }
        public string LongName { get; private set; }
        public string HelpText { get; set; }
        public bool Required { get; set; }
        public int Index { get; set; }

        public OptionAttribute(string shortName = null, string longName = null)
        {
            ShortName = shortName;
            LongName = longName;
            Index = -1;
        }
    }
}
