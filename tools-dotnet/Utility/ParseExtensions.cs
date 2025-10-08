using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace tools_dotnet.Utility
{
    /// <summary>
    /// Small helper functions to help convert strings to longs or UUIDs in Sieve customer methods
    /// </summary>
    public static class ParseExtensions
    {
        /// <summary>
        /// Throw, IgnoreValue, ReturnNull or ReturnEmpty
        /// </summary>
        public enum ParseFailureBehavior
        {
            /// <summary>
            /// Throw a FormatException on first failure
            /// </summary>
            Throw,

            /// <summary>
            /// Skip the bad value, continue parsing
            /// </summary>
            IgnoreValue,

            /// <summary>
            /// Return null on first failure
            /// </summary>
            ReturnNull,

            /// <summary>
            /// If any failure occurred, return an empty list
            /// </summary>
            ReturnEmpty
        }

        /// <summary>
        /// Parses an enumerable of strings into a typed list (int, long, Guid).
        /// Uses CultureInfo.InvariantCulture as default, but can take current locale
        /// </summary>
        public static List<T>? ParseValues<T>(
            IEnumerable<string> values,
            ParseFailureBehavior onFailure = ParseFailureBehavior.ReturnNull,
            IFormatProvider? provider = null)
            where T : IParsable<T>
        {
            provider ??= CultureInfo.InvariantCulture;

            //this is WAY more memory-efficient and even faster than new List<>(); (benchmarked)
            values.TryGetNonEnumeratedCount(out var count);
            var result = new List<T>(count);

            foreach (var raw in values)
            {
                if (T.TryParse(raw, provider, out var parsed))
                {
                    result.Add(parsed);
                }
                else
                {
                    switch (onFailure)
                    {
                        case ParseFailureBehavior.Throw:
                            throw new FormatException($"Could not parse '{raw}' as {typeof(T).Name}.");
                        case ParseFailureBehavior.IgnoreValue:
                            continue;
                        case ParseFailureBehavior.ReturnNull:
                            return null;

                        case ParseFailureBehavior.ReturnEmpty:
                            //this is faster than return new List<>(); (benchmarked)
                            result.Clear();
                            return result;
                    }
                }
            }

            return result;
        }
    }
}