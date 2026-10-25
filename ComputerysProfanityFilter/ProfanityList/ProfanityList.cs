using System;
using System.Buffers;
using System.Collections.Generic;

namespace ComputerysProfanityFilter {
    /// <summary>
    /// Finds and censors configured profanity terms in text.
    /// </summary>
    public sealed partial class ProfanityList {
        private const int MinimumCensorBufferSize = 256;

        /// <summary>
        /// Terms that are exempt from censorship when they appear without repeated characters.
        /// </summary>
        private readonly string[] _allowTerms;

        /// <summary>
        /// Longest term length
        /// </summary>
        private readonly int _longestTermLength;

        /// <summary>
        /// Characters, typically letters, that are expected to make up words.
        /// Characters not in this list will be speculatively treated as their own chunk even without whitespace.
        /// </summary>
        private readonly HashSet<char> _expectedCharacters;

        /// <summary>
        /// Characters that will be ignored within a term.
        /// </summary>
        private readonly HashSet<char> _joinerCharacters;

        /// <summary>
        /// Characters that split terms while preserving punctuation-obfuscation matching.
        /// Character and sequence mappings take precedence when they directly continue a term.
        /// </summary>
        private readonly HashSet<char> _boundaryCharacters;

        /// <summary>
        /// Maps individual characters to their replacement strings.
        /// </summary>
        private readonly Dictionary<char, string> _characterMap;

        /// <summary>
        /// Maps character sequences to their replacement strings and literal always-censor terms.
        /// </summary>
        private readonly PrefixMatcher _prefixMatcher;

        /// <summary>
        /// Replaces every detected profanity match with the specified character.
        /// </summary>
        /// <param name="input">The text to censor.</param>
        /// <param name="censorCharacter">The character used to replace matched characters.</param>
        /// <returns>The input text with detected profanity matches censored.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        public string Censor(string input, char censorCharacter = '#') {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (input.Length == 0) { return input; }

            CensorMatchHandler handler = new CensorMatchHandler(input, censorCharacter);
            try { handler = ScanForProfanity(input, handler); return handler.GetResult(); }
            finally { handler.ReturnBuffer(); }
        }

        private struct CensorMatchHandler : IMatchHandler {
            private readonly string _input;
            private readonly char _censorCharacter;
            private char[]? _censoredCharacters; // TODO: use a span with a stack allocated backing given the length is shorter than it should be

            internal CensorMatchHandler(string input, char censorCharacter) {
                _input = input; _censorCharacter = censorCharacter;
                _censoredCharacters = null;
            }

            public bool HandleMatch(int start, int end, string term) {
                if (_censoredCharacters == null) {
                    _censoredCharacters = ArrayPool<char>.Shared.Rent(Math.Max(_input.Length, MinimumCensorBufferSize));
                    _input.CopyTo(0, _censoredCharacters, 0, _input.Length);
                }
                for (int index = start; index <= end; index++) {
                    _censoredCharacters[index] = _censorCharacter;
                }
                return false;
            }
            internal string GetResult() => _censoredCharacters == null ? _input : new string(_censoredCharacters, 0, _input.Length);

            internal void ReturnBuffer() {
                if (_censoredCharacters == null) { return; }
                ArrayPool<char>.Shared.Return(_censoredCharacters);
                _censoredCharacters = null;
            }
        }

        /// <summary>
        /// Checks whether the input contains at least one configured profanity term.
        /// </summary>
        /// <param name="input">The text to check.</param>
        /// <returns><see langword="true"/> if the input contains a match; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        public bool HasProfanity(string input) {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (input.Length == 0) { return false; }

            FirstMatchHandler handler = ScanForProfanity(input, default(FirstMatchHandler));
            return handler.Found;
        }

        private struct FirstMatchHandler : IMatchHandler {
            internal bool Found;
            public bool HandleMatch(int start, int end, string term) {
                Found = true;
                return true;
            }
        }


        /// <summary>
        /// Finds every configured profanity match in the input.
        /// </summary>
        /// <param name="input">The text to scan.</param>
        /// <returns>A read-only list of matches, or an empty list when no matches are found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        public IReadOnlyList<ProfanityMatch> DetectAllProfanities(string input) {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }
            if (input.Length == 0) { return Array.Empty<ProfanityMatch>(); }

            List<ProfanityMatch> matches = new List<ProfanityMatch>();
            CollectMatchesHandler handler = new CollectMatchesHandler(matches);
            ScanForProfanity(input, handler);
            return matches.Count == 0 ? Array.Empty<ProfanityMatch>() : matches.AsReadOnly();
        }

        private readonly struct CollectMatchesHandler : IMatchHandler {
            private readonly List<ProfanityMatch> _matches;
            internal CollectMatchesHandler(List<ProfanityMatch> matches) { _matches = matches; }
            public bool HandleMatch(int start, int end, string term) {
                _matches.Add(new ProfanityMatch(start, end + 1, term));
                return false;
            }
        }
    }
}
