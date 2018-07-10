using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClosureExterns.Extensions
{
    public static class StringExtensions
    {
        public static string DefaultIfEmpty(this string value, string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static string ToCamelCase(this string value)
        {
            // TODO: Improve the behavior, it should be able to change casing to lower based on words.
            return value.Substring(0, 1).ToLowerInvariant() + value.Substring(1);
        }

        public static string TrimEndNewLine(this string value)
        {
            return value.TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}
