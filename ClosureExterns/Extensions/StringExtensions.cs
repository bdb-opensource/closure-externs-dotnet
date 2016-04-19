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
    }
}
