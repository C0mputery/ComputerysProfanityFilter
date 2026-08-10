using System.Collections.Generic;

namespace ComputerysProfanityFilter {
    internal static class StringHelper {
        internal static string[] SplitOnWhitespace(string value) {
            List<string> tokens = new List<string>();
            int tokenStart = 0;

            for (int index = 0; index <= value.Length; index++) {
                if (index < value.Length && !char.IsWhiteSpace(value[index])) { continue; }
                if (tokenStart < index) { tokens.Add(value.Substring(tokenStart, index - tokenStart)); }
                tokenStart = index + 1;
            }

            return tokens.ToArray();
        }

        internal static bool ContainsWhitespace(string value) {
            foreach (char character in value) {
                if (char.IsWhiteSpace(character)) { return true; }
            }

            return false;
        }

    }
}
