using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLineParser.Attribute;
using CommandLineParser.Reflection;

namespace CommandLineParser.Parser
{
    public class Parser<T>
        where T : class, new()
    {
        private readonly List<ParseRule> _rulesTable;
        private readonly ArgumentParser<T> _parser;

        public Parser()
        {
            _rulesTable = BuildParseRules();
            _parser = new ArgumentParser<T>(_rulesTable);
        }

        static List<ParseRule> BuildParseRules()
        {
            List<ParseRule> rules = ReflectionExtensions
                .GetAttributes<T, OptionAttribute>()
                .Select(x => new ParseRule
                {
                    Property = x.Key,
                    Option = x.Value
                })
                .ToList();
            return rules;
        }

        public T Parse(string input)
        {
            string[] arguments = input.Split(' ');
            T result = Parse(arguments);
            return result;
        }

        public T Parse(string[] arguments)
        {
            _parser.Parse(arguments);

            T result = BuildResult();
            return result;
        }

        private T BuildResult()
        {
            T result = new T();
            IEnumerable<ParseRule> parsedRules = _rulesTable
                .Where(x => x.ParsedValue != null);

            foreach (var entry in parsedRules)
            {
                result.SetProperty(entry.Property.Name, entry.ParsedValue);
            }

            return result;
        }
    }
}
