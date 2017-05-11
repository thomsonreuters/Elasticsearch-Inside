using System;

namespace ElasticsearchInside.CommandLine
{
    public class FormattedArgumentAttribute : Attribute
    {
        private readonly string _argumentName;
        private readonly object _defaultValue;
        private readonly bool _skipIfNull;

        public FormattedArgumentAttribute(string argumentName, object defaultValue = null, bool skipIfNull = false)
        {
            _argumentName = argumentName;
            _defaultValue = defaultValue;
            _skipIfNull = skipIfNull;
        }

        public string ArgumentName
        {
            get { return _argumentName; }
        }

        public object DefaultValue
        {
            get { return _defaultValue; }
        }

        public bool SkipIfNull
        {
            get { return _skipIfNull; }
        }
    }
}