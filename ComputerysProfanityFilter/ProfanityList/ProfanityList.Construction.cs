using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        public ProfanityList() : this(DefaultProfanityList.Terms) { }
        public ProfanityList(IEnumerable<string> terms, bool expandTermForms = true) : this(terms, DefaultProfanityList.AllowTerms, expandTermForms) { }

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
                if (normalizedAllowTerm.Length > _maximumAllowTermLength) {
                    _maximumAllowTermLength = normalizedAllowTerm.Length;
                }
            }
            _allowTerms = new string[uniqueAllowTerms.Count];
            uniqueAllowTerms.CopyTo(_allowTerms);

            HashSet<string> encodedTerms = expandTermForms ? GenerateEncodedVariations(terms) : EncodeTermHashSet(terms);
            if (encodedTerms.Count == 0) {
                throw new ArgumentException("At least one term that produces a non-empty pattern is required.", nameof(terms));
            }

            InitializeAhoCorasick(encodedTerms);
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
