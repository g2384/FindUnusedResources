using System;
using System.Collections.Generic;
using System.Linq;

namespace FindUnusedResources
{
    public static class StringExtensions
    {
        public static IEnumerable<string> Split(this string value, string separator)
        {
            return value.Split(new[] {separator}, StringSplitOptions.None).ToList();
        }
    }
}
