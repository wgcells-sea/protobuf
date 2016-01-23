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
        private const char SEPARATOR = ' ';
        private const char EQUAL_MARKER = '=';
        private const string SINGLE_HYPHEN = "-";
        private const string DOUBLE_HYPHEN = "--";
        private readonly Dictionary<string, ParseRule> _rulesTable;
        private readonly Dictionary<ParseState, Func<string, string>> _parseMap; 
        private ParseState _state;
        private ParseRule _currentRule;

        private static readonly Lazy<Parser> _instance = new Lazy<Parser>(() => new Parser());

        public static Parser Instance
        {
            get { return _instance.Value; }
        }

        public Parser()
        {
            _parseMap = new Dictionary<ParseState, Func<string, string>>
            {
                { ParseState.IndexedOption, ParseIndexedOption},
                { ParseState.GenericOption, ParseGenericOption},
                { ParseState.ShortOption, ParseShortOption},
                { ParseState.LongOption, ParseLongOption},
                { ParseState.BooleanOption, ParseBooleanOption},
                { ParseState.Value, ParseValue}
            };
            _rulesTable = new Dictionary<string, ParseRule>();
            _state = ParseState.None;
        }

        public void SetParseRules(List<ParseRule> rules)
        {
            foreach (var rule in rules)
            {
                if (rule.Option.ShortName != null)
                {
                    _rulesTable.Add(rule.Option.ShortName, rule);
                }

                if (rule.Option.LongName != null)
                {
                    _rulesTable.Add(rule.Option.LongName, rule);
                }
            }
        }

        public T Parse<T>(string[] arguments) where T : class, new()
        {
            foreach (var argument in arguments)
            {
                ParseArgument(argument);
            }

            T result = BuildResult<T>();

            return result;
        }

        public T Parse<T>(string input) where T : class, new()
        {
            string[] arguments = input.Split(SEPARATOR);
            T result = Parse<T>(arguments);

            return result;
        }

        private T BuildResult<T>() where T : class, new()
        {
            T result = new T();
            IEnumerable<ParseRule> parsedRules = _rulesTable
                .Where(x => x.Value != null)
                .Select(x=>x.Value);

            foreach (var entry in parsedRules)
            {
                result.SetProperty(entry.Property.Name, entry.Value);
            }

            return result;
        }

        private void ParseArgument(string argument)
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
            ParseRule priorityOption = _rulesTable
                .Where(x => x.Value.Option.Index >= 0 && x.Value.Value == null)
                .Select(x => x.Value)
                .OrderBy(x => x.Option.Index)
                .FirstOrDefault();

            ParseRule booleanOption = _rulesTable
                .Where(x => x.Value.Property.PropertyType.IsBoolean())
                .Select(x => x.Value)
                .OrderBy(x=>x.Option.LongName == null ? x.Option.ShortName.Length : x.Option.LongName.Length)
                .FirstOrDefault(x => IsCandidate(argument, x.Option));

            ParseRule regularOption = _rulesTable
                .Where(x => !x.Value.Property.PropertyType.IsBoolean())
                .Select(x => x.Value)
                .OrderBy(x => x.Option.LongName == null ? x.Option.ShortName.Length : x.Option.LongName.Length)
                .FirstOrDefault(x => IsCandidate(argument, x.Option));

            if (priorityOption != null)
                return ParseState.IndexedOption;

            if (booleanOption != null)
                return ParseState.BooleanOption;

            if (argument.StartsWith(DOUBLE_HYPHEN))
                return ParseState.LongOption;

            if (argument.StartsWith(SINGLE_HYPHEN))
                return ParseState.ShortOption;

            if (argument.IndexOf(EQUAL_MARKER) > 0 || regularOption!=null)
                return ParseState.GenericOption;

            return ParseState.Value;
        }

        private string ParseIndexedOption(string argument)
        {
            ParseRule priorityOption = _rulesTable
                .Where(x => x.Value.Option.Index>=0 && x.Value.Value == null)
                .Select(x => x.Value)
                .OrderBy(x => x.Option.Index)
                .FirstOrDefault();

            if (priorityOption != null)
            {
                _currentRule = priorityOption;

                /*
                TODO: discuss if we can start specifying indexed properties by name or just by value
                if (argument.StartsWith(SINGLE_HYPHEN))
                    return ParseShortOption(argument);

                if (argument.StartsWith(DOUBLE_HYPHEN))
                    return ParseLongOption(argument);
                */
                if (argument.StartsWith(SINGLE_HYPHEN) || argument.StartsWith(DOUBLE_HYPHEN))
                    throw new ArgumentException(string.Format("Argument {0} is not valid for a indexed based option", argument));

                _currentRule.Value = Convert.ChangeType(argument, _currentRule.Property.PropertyType);
                _currentRule = null;
            

                return string.Empty;
            }

            throw new InvalidOperationException("No indexed option was found");
        }

        private string ParseGenericOption(string argument)
        {
            string result = ParseOption(argument);

            return result;
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
            var matchingRule = _rulesTable
                .Where(x => argument.StartsWith(x.Key))
                .OrderByDescending(x => x.Key.Length)
                .FirstOrDefault();

            if (matchingRule.Equals(default(KeyValuePair<string, ParseRule>)))
                throw new ArgumentException(string.Format("Argument {0} is not mapped to any option", argument));

            var isBoolean = matchingRule.Value.Property.PropertyType.IsBoolean();
            argument = argument.Substring(matchingRule.Key.Length);
            _currentRule = matchingRule.Value;
            _state = isBoolean
                ? ParseState.BooleanOption
                : ParseState.Value;
            
            return argument;
        }

        private string ParseBooleanOption(string argument)
        {
            ParseRule booleanOption = _rulesTable
                .Where(x => x.Value.Property.PropertyType.IsBoolean())
                .Select(x => x.Value)
                .OrderBy(x => x.Option.LongName == null ? x.Option.ShortName.Length : x.Option.LongName.Length)
                .First(x => IsCandidate(argument, x.Option));

            _currentRule = booleanOption;
            _currentRule.Value = true;
            _currentRule = null;
            _state = ParseState.None;

            if (booleanOption.Option.LongName != null && argument.StartsWith(DOUBLE_HYPHEN + booleanOption.Option.LongName))
            {
                argument = argument.Substring((DOUBLE_HYPHEN + booleanOption.Option.LongName).Length);
            }
            else if(booleanOption.Option.ShortName != null && argument.StartsWith(SINGLE_HYPHEN + booleanOption.Option.ShortName))
            {
                argument = argument.Substring((SINGLE_HYPHEN + booleanOption.Option.ShortName).Length);
            }

            return argument;
        }

        private string ParseValue(string argument)
        {
            if (_currentRule == null) 
                throw new InvalidOperationException(string.Format("There is not option for value {0}", argument));

            TypeConverter converter = TypeDescriptor.GetConverter(_currentRule.Property.PropertyType);

            argument = argument.Trim(EQUAL_MARKER);
            argument = argument.Trim();

            if (!converter.IsValid(argument))
                throw new InvalidOperationException(string.Format("The provided value {0} is not assignable to option {1}", argument, _currentRule.Option.ShortName ?? _currentRule.Option.LongName));
            
            _currentRule.Value = Convert.ChangeType(argument, _currentRule.Property.PropertyType);
            _currentRule = null;

            return null;
        }

        private bool IsCandidate(string argument, OptionAttribute option)
        {
            if (option.LongName != null)
            {
                if (argument.StartsWith(option.LongName) ||
                argument.StartsWith(SINGLE_HYPHEN + option.LongName) ||
                argument.StartsWith(DOUBLE_HYPHEN + option.LongName))
                    return true;
            } 

            if (argument.StartsWith(option.ShortName) ||
                argument.StartsWith(SINGLE_HYPHEN + option.ShortName) ||
                argument.StartsWith(DOUBLE_HYPHEN + option.ShortName))
                return true;

            return false;
        }

        private enum ParseState
        {
            None,
            IndexedOption,
            GenericOption,
            ShortOption,
            LongOption,
            BooleanOption,
            Value
        }
    }
}
