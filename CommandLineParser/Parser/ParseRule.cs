using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLineParser.Attribute;

namespace CommandLineParser.Parser
{
    public class ParseRule
    { 
        public OptionAttribute Option { get; set; }
        public PropertyInfo Property { get; set; }
        public object Value { get; set; }
    }
}
