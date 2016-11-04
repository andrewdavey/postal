using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Postal
{
    /// <summary>
    /// Helper methods for parsing email.
    /// </summary>
    public static class ParserUtils
    {
        /// <summary>
        /// Headers are of the form "(key): (value)" e.g. "Subject: Hello, world".
        /// The headers block is terminated by an empty line.
        /// </summary>
        public static void ParseHeaders(TextReader reader, Action<string, string> useKeyAndValue)
        {
            string line;
            while (string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                // Skip over any empty lines before the headers.
            }

            var headerStart = new Regex(@"^\s*([A-Za-z\-]+)\s*:\s*(.*)");
            do
            {
                var match = headerStart.Match(line);
                if (!match.Success) break;

                var key = match.Groups[1].Value.ToLowerInvariant();
                var value = match.Groups[2].Value.TrimEnd();
                if (!string.IsNullOrWhiteSpace(value)) {
                  useKeyAndValue(key, value);
                }
            } while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()));
        }
    }
}
