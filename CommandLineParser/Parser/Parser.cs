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
    public class Parser
    {
        private const string EQUAL_MARKER = "=";
        private const string SINGLE_HYPHEN = "-";
        private const string DOUBLE_HYPHEN = "--";
        private readonly Dictionary<string, ParseEntry> _rulesTable;
        private readonly Dictionary<ParseState, Func<string, string>> _parseMap; 
        private ParseState _state;
        private ParseEntry _currentEntry;

        private static readonly Lazy<Parser> _instance = new Lazy<Parser>(() => new Parser());

        public static Parser Instance
        {
            get { return _instance.Value; }
        }

        private Parser()
        {
            _parseMap = new Dictionary<ParseState, Func<string, string>>
            {
                { ParseState.PriorityOption, ParsePriorityOption},
                { ParseState.ShortOption, ParseShortOption},
                { ParseState.LongOption, ParseLongOption},
                { ParseState.Value, ParseValue}
            };
            _rulesTable = new Dictionary<string, ParseEntry>();
            _state = ParseState.None;
        }

        public void SetParseRules(List<ParseEntry> rules)
        {
            foreach (var rule in rules)
            {
                _rulesTable[rule.Option.ShortName] = rule;
                _rulesTable[rule.Option.LongName] = rule;
            }
        }

        public T Parse<T>(string[] arguments) where T : class
        {
            return null;
        }

        public void ParseArgument(string argument)
        {
            if (argument == null) 
                throw new ArgumentNullException("argument");

            while (!string.IsNullOrEmpty(argument))
            {
                _state = GetTransition(argument);
                argument = _parseMap[_state](argument);
            }
        }

        private ParseState GetTransition(string argument)
        {
            ParseEntry priorityOption = _rulesTable
                .Where(x => x.Value.Option.Index>=0 && x.Value.Value == null)
                .Select(x => x.Value)
                .OrderBy(x => x.Option.Index)
                .FirstOrDefault();

            if (priorityOption != null)
                return ParseState.PriorityOption;
            
            if (argument.StartsWith(DOUBLE_HYPHEN))
                return ParseState.LongOption;

            if (argument.StartsWith(SINGLE_HYPHEN))
                return ParseState.ShortOption;

            return ParseState.Value;
        }

        private string ParsePriorityOption(string argument)
        {
            ParseEntry priorityOption = _rulesTable
                .Where(x => x.Value.Option.Index>=0 && x.Value.Value == null)
                .Select(x => x.Value)
                .OrderBy(x => x.Option.Index)
                .FirstOrDefault();

            if (priorityOption != null)
            {
                _currentEntry = priorityOption;
                _currentEntry.Value = Convert.ChangeType(argument, _currentEntry.Property.PropertyType);
                _currentEntry = null;

                return null;
            }

            return argument;
        }

        private string ParseShortOption(string argument)
        {
            argument = argument.Substring(SINGLE_HYPHEN.Length);
            string result = ParseOption(argument);

            return result;
        }

        private string ParseLongOption(string argument)
        {
            argument = argument.Substring(DOUBLE_HYPHEN.Length);
            string result = ParseOption(argument);

            return result;
        }

        private string ParseOption(string argument)
        {
            foreach (var option in _rulesTable)
            {
                if (argument.StartsWith(option.Key))
                {
                    argument = argument.Substring(option.Key.Length);
                    _currentEntry = option.Value;
                    _state = option.Value.Property.PropertyType.IsBoolean() ?
                        ParseState.None :
                        ParseState.Value;

                    return argument;
                }
            }

            throw new ArgumentException(string.Format("Argument {0} is not mapped to any option", argument));
        }

        private string ParseValue(string argument)
        {
            if (_currentEntry == null) 
                throw new InvalidOperationException(string.Format("There is not option for value {0}", argument));

            TypeConverter converter = TypeDescriptor.GetConverter(_currentEntry.Property.PropertyType);

            if (!converter.IsValid(argument))
                throw new InvalidOperationException(string.Format("The provided value {0} is not assignable to option {1}", argument, _currentEntry.Option.ShortName ?? _currentEntry.Option.LongName));
            
            _currentEntry.Value = Convert.ChangeType(argument, _currentEntry.Property.PropertyType);
            _currentEntry = null;

            return null;
        }

        private enum ParseState
        {
            None,
            PriorityOption,
            ShortOption,
            LongOption,
            Value
        }
    }
}
