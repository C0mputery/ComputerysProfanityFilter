using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComputerysProfanityFilter {
    internal static class StringHelper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string[] SplitOnWhitespace(string value) {
            List<string> tokens = new List<string>();
            int tokenStart = 0;
            int valueLength = value.Length;

            for (int index = 0; index <= valueLength; index++) {
                if (index < valueLength && !char.IsWhiteSpace(value[index])) { continue; }
                if (tokenStart < index) { tokens.Add(value.Substring(tokenStart, index - tokenStart)); }
                tokenStart = index + 1;
            }

            return tokens.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsWhitespace(ReadOnlySpan<char> value) {
            foreach (char character in value) {
                if (char.IsWhiteSpace(character)) { return true; }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsConsonant(char c) => c is >= 'a' and <= 'z' and not ('a' or 'e' or 'i' or 'o' or 'u');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EndsWithConsonantVowelConsonant(ReadOnlySpan<char> word) {
            if (word.Length < 3) { return false; }

            char lastCharacter = word[^1];
            return IsConsonant(word[^3]) && IsVowel(word[^2]) && IsConsonant(lastCharacter) && lastCharacter is not ('w' or 'x' or 'y');
        }
    }
}
