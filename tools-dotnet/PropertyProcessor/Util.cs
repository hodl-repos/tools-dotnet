using System;
using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.PropertyProcessor
{
    internal static class Util
    {
        /// <summary>
        /// validates if (){}[]"" are opened and closed correctly
        /// returns null when everything is good
        /// returns index-position when something is fishy
        /// </summary>
        internal static long? ValidatePunctuation(string input)
        {
            char[] openPunctuations = new char[] { '(', '{', '[' };
            char[] closePunctuations = new char[] { ')', '}', ']' };

            List<int> inputIndexes = new List<int>();
            List<int> openPuctuationIndexes = new List<int>();
            bool punctuationIsOpen = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (!punctuationIsOpen && input[i] == '"')
                {
                    inputIndexes.Add(i); //remember if never closed
                    punctuationIsOpen = true;
                }
                else if (punctuationIsOpen && input[i] == '"' && input[i - 1] != '\\')
                {
                    inputIndexes.RemoveAt(inputIndexes.Count - 1);
                    punctuationIsOpen = false;
                }

                //special handling for " -> this is a string, everything can be inside
                if (punctuationIsOpen)
                {
                    continue;
                }

                if (openPunctuations.Contains(input[i]))
                {
                    openPuctuationIndexes.Add(Array.IndexOf(openPunctuations, input[i]));
                    inputIndexes.Add(i);
                }
                else if (closePunctuations.Contains(input[i]))
                {
                    if (openPuctuationIndexes.Count == 0) //no items to check agains
                    {
                        return i;
                    }

                    var expectedLastIndex = Array.IndexOf(closePunctuations, input[i]);

                    if (openPuctuationIndexes[openPuctuationIndexes.Count - 1] != expectedLastIndex)
                    {
                        return i;
                    }
                    else
                    {
                        openPuctuationIndexes.RemoveAt(openPuctuationIndexes.Count - 1);
                        inputIndexes.RemoveAt(inputIndexes.Count - 1);
                    }
                }
            }

            //check if something was never closed
            if (inputIndexes.Count > 0)
            {
                return inputIndexes[0];
            }

            return null;
        }

        internal static string RemovesWhitespaces(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            List<char> output = new List<char>();

            bool punctuationIsOpen = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (!punctuationIsOpen && input[i] == '"')
                {
                    punctuationIsOpen = true;
                    output.Add(input[i]);
                }
                else if (punctuationIsOpen && input[i] == '"' && input[i - 1] != '\\') //i > 0 check is not necessary -> cannot be the case
                {
                    punctuationIsOpen = false;
                    output.Add(input[i]);
                }
                else if (punctuationIsOpen || !char.IsWhiteSpace(input[i])) //always add char to list when puntiuation is open
                {
                    output.Add(input[i]);
                }
            }

            return new string(output.ToArray());
        }
    }
}