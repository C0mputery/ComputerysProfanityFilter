using System;
using System.Collections.Generic;
using System.Linq;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        public readonly char Boundary;
        private readonly Dictionary<char, string> _characterMap;
        private readonly HashSet<char> _ignorableCharacters;
        private readonly HashSet<char> _allowsDouble;
        private readonly Dictionary<char, SequenceMapping[]> _sequenceMappingsByFirstCharacter;
        private readonly AhoCorasickMatcher _matcher;

        public ProfanityList() : this(
            DefaultProfanityList.Boundary, DefaultProfanityList.CharacterMap, DefaultProfanityList.SequenceMap,
            DefaultProfanityList.IgnorableCharacters, DefaultProfanityList.AllowsDouble, DefaultProfanityList.Words
        ) { }

        public ProfanityList(
            char boundary, IDictionary<char, string> characterMap, IDictionary<string, string> sequenceMap,
            IEnumerable<char> ignorableCharacters, IEnumerable<char> allowsEnglishDouble, IEnumerable<string> words,
            bool expandWordForms = true
        ) {
            if (characterMap is null) { throw new ArgumentNullException(nameof(characterMap)); }
            if (sequenceMap is null) { throw new ArgumentNullException(nameof(sequenceMap)); }
            if (ignorableCharacters is null) { throw new ArgumentNullException(nameof(ignorableCharacters)); }
            if (allowsEnglishDouble is null) { throw new ArgumentNullException(nameof(allowsEnglishDouble)); }
            if (words is null) { throw new ArgumentNullException(nameof(words)); }

            Boundary = boundary;
            _characterMap = new Dictionary<char, string>(characterMap);
            Dictionary<string, string> sequenceMap1 = new Dictionary<string, string>(sequenceMap);
            _ignorableCharacters = new HashSet<char>(ignorableCharacters);
            _allowsDouble = new HashSet<char>(allowsEnglishDouble);
            _sequenceMappingsByFirstCharacter = BuildSequenceIndex(sequenceMap1);
            IEnumerable<string> patterns = expandWordForms ? PopulateVariations(words) : PopulateEncodedWords(words);
            _matcher = new AhoCorasickMatcher(patterns, Boundary);
        }

        private HashSet<string> PopulateEncodedWords(IEnumerable<string> words) {
            HashSet<string> encodedWords = new HashSet<string>();
            foreach (string word in words) {
                if (!string.IsNullOrEmpty(word)) { PopulateEncodedWord(word, encodedWords); }
            }
            return encodedWords;
        }

        private static Dictionary<char, SequenceMapping[]> BuildSequenceIndex(IEnumerable<KeyValuePair<string, string>> sequenceMap) {
            Dictionary<char, List<SequenceMapping>> groups = new Dictionary<char, List<SequenceMapping>>();
            foreach (KeyValuePair<string, string> sequence in sequenceMap) {
                if (sequence.Key.Length == 0) { continue; }

                char initial = char.ToUpperInvariant(sequence.Key[0]);
                if (!groups.TryGetValue(initial, out List<SequenceMapping>? mappings)) {
                    mappings = new List<SequenceMapping>();
                    groups.Add(initial, mappings);
                }
                mappings.Add(new SequenceMapping(sequence.Key, sequence.Value));
            }

            Dictionary<char, SequenceMapping[]> index = new Dictionary<char, SequenceMapping[]>(groups.Count);
            foreach ((char key, List<SequenceMapping> value) in groups) {
                index.Add(key, value.OrderByDescending(mapping => mapping.Key.Length).ToArray());
            }
            return index;
        }
    }
}
