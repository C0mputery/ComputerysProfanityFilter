using System.Collections.Generic;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        /// <summary>
        /// Terms that are exempt from censorship when they appear without repeated characters.
        /// </summary>
        private readonly string[] _allowTerms;

        private readonly int _maximumAllowTermLength;

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
        ///
        /// </summary>
        private readonly Dictionary<char, string> _characterMap;

        /// <summary>
        ///
        /// </summary>
        private readonly SequenceTrie _sequenceMap;
    }
}
