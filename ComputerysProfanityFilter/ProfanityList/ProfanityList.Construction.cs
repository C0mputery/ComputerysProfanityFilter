using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        /// <summary>
        /// Creates a profanity list using the default terms and matching settings.
        /// </summary>
        public ProfanityList() : this(DefaultProfanityList.Terms) { }

        /// <summary>
        /// Creates a profanity list using the supplied terms and default matching settings.
        /// </summary>
        /// <param name="terms">The terms to detect.</param>
        /// <param name="expandTermForms">Whether to include common plural, tense, and suffix forms.</param>
        /// <exception cref="ArgumentNullException"><paramref name="terms"/> is <see langword="null"/>.</exception>
        public ProfanityList(IEnumerable<string> terms, bool expandTermForms = true) : this(terms, DefaultProfanityList.AllowTerms, expandTermForms) { }

        /// <summary>
        /// Creates a profanity list using the supplied terms and allow terms with default matching settings.
        /// </summary>
        /// <param name="terms">The terms to detect.</param>
        /// <param name="allowTerms">Terms that should not match when they appear without repeated characters.</param>
        /// <param name="expandTermForms">Whether to include common plural, tense, and suffix forms.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="terms"/> or <paramref name="allowTerms"/> is <see langword="null"/>.
        /// </exception>
        public ProfanityList(IEnumerable<string> terms, IEnumerable<string> allowTerms, bool expandTermForms = true) : this(
            terms,
            allowTerms,
            expandTermForms,
            DefaultProfanityList.ExpectedCharacters,
            DefaultProfanityList.JoinerCharacters,
            DefaultProfanityList.BoundaryCharacters,
            DefaultProfanityList.CharacterMap,
            DefaultProfanityList.SequenceMap
        ) { }

        /// <summary>
        /// Creates a profanity list with fully customized matching settings.
        /// </summary>
        /// <param name="terms">The terms to detect.</param>
        /// <param name="allowTerms">Terms that should not match when they appear without repeated characters.</param>
        /// <param name="expandTermForms">Whether to include common plural, tense, and suffix forms.</param>
        /// <param name="expectedCharacters">Characters that are normally part of words.</param>
        /// <param name="joinerCharacters">Characters to ignore when they appear within a term.</param>
        /// <param name="boundaryCharacters">Characters that split terms while preserving obfuscation matching.</param>
        /// <param name="characterMap">Mappings from individual characters to replacement strings.</param>
        /// <param name="sequenceMap">Mappings from character sequences to replacement strings.</param>
        /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="joinerCharacters"/> and <paramref name="boundaryCharacters"/> overlap, or no supplied term produces a matchable pattern.
        /// </exception>
        public ProfanityList(
            IEnumerable<string> terms,
            IEnumerable<string> allowTerms,
            bool expandTermForms,
            IEnumerable<char> expectedCharacters,
            IEnumerable<char> joinerCharacters,
            IEnumerable<char> boundaryCharacters,
            IEnumerable<KeyValuePair<char, string>> characterMap,
            IEnumerable<KeyValuePair<string, string>> sequenceMap
        ) {
            if (terms == null) { throw new ArgumentNullException(nameof(terms)); }
            if (allowTerms == null) { throw new ArgumentNullException(nameof(allowTerms)); }
            if (expectedCharacters == null) { throw new ArgumentNullException(nameof(expectedCharacters)); }
            if (joinerCharacters == null) { throw new ArgumentNullException(nameof(joinerCharacters)); }
            if (boundaryCharacters == null) { throw new ArgumentNullException(nameof(boundaryCharacters)); }
            if (characterMap == null) { throw new ArgumentNullException(nameof(characterMap)); }
            if (sequenceMap == null) { throw new ArgumentNullException(nameof(sequenceMap)); }

            _expectedCharacters = NormalizedCharHashSet(expectedCharacters);
            _joinerCharacters = NormalizedCharHashSet(joinerCharacters);
            _boundaryCharacters = NormalizedCharHashSet(boundaryCharacters);
            if (_joinerCharacters.Overlaps(_boundaryCharacters)) {
                throw new ArgumentException("Joiner and boundary characters must not overlap.");
            }
            _characterMap = new Dictionary<char, string>();
            foreach (KeyValuePair<char, string> characterMapEntry in characterMap) {
                _characterMap[char.ToLowerInvariant(characterMapEntry.Key)] = CollapseRepeatedCharacters(characterMapEntry.Value);
            }

            _sequenceMap = new SequenceTrie();
            foreach (KeyValuePair<string, string> sequenceMapEntry in sequenceMap) {
                _sequenceMap.Add(sequenceMapEntry.Key, CollapseRepeatedCharacters(sequenceMapEntry.Value));
            }

            HashSet<string> uniqueAllowTerms = new HashSet<string>(StringComparer.Ordinal);
            foreach (string allowTerm in allowTerms) {
                string normalizedAllowTerm = allowTerm.ToLowerInvariant();
                uniqueAllowTerms.Add(normalizedAllowTerm);
                if (normalizedAllowTerm.Length > _longestTermLength) {
                    _longestTermLength = normalizedAllowTerm.Length;
                }
            }
            _allowTerms = new string[uniqueAllowTerms.Count];
            uniqueAllowTerms.CopyTo(_allowTerms);

            List<string> sourceTerms = new List<string>();
            foreach (string term in terms) { sourceTerms.Add(term); }
            Dictionary<string, Pattern> encodedTerms = expandTermForms ? GenerateEncodedVariations(sourceTerms) : EncodeTerms(sourceTerms);
            if (encodedTerms.Count == 0) {
                throw new ArgumentException("At least one term that produces a non-empty pattern is required.", nameof(terms));
            }

            InitializeAhoCorasick(encodedTerms.Values);
        }

        private Dictionary<string, Pattern> EncodeTerms(IReadOnlyList<string> terms) {
            Dictionary<string, Pattern> encodedTerms = new Dictionary<string, Pattern>(StringComparer.Ordinal);
            for (int index = 0; index < terms.Count; index++) { AddEncodedPattern(EncodeTerm(terms[index].AsSpan()), terms[index], index, encodedTerms); }
            return encodedTerms;
        }

        private static void AddEncodedPattern(string encoded, string term, int termOrder, Dictionary<string, Pattern> patterns) {
            Pattern pattern = new Pattern(encoded, term, termOrder);
            if (!patterns.TryGetValue(encoded, out Pattern? existing) || pattern.IsPreferredTo(existing)) { patterns[encoded] = pattern; }
        }

        private static HashSet<char> NormalizedCharHashSet(IEnumerable<char> terms) {
            HashSet<char> normalizedChars = new HashSet<char>();
            foreach (char c in terms) { normalizedChars.Add(char.ToLowerInvariant(c)); }
            return normalizedChars;
        }

        private string CollapseRepeatedCharacters(string value) {
            StringBuilder normalized = new StringBuilder(value.Length);
            char previous = '\0';

            foreach (char character in value) {
                char normalizedCharacter = char.ToLowerInvariant(character);
                if (normalizedCharacter == previous) { continue; }

                previous = normalizedCharacter;
                normalized.Append(normalizedCharacter);
            }

            return normalized.ToString();
        }
    }
}
