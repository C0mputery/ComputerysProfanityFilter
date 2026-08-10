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
            DefaultProfanityList.IgnorableCharacters, DefaultProfanityList.AllowsDouble, DefaultProfanityList.Entries
        ) { }

        public ProfanityList(
            char boundary, IEnumerable<KeyValuePair<char, string>> characterMap, IEnumerable<KeyValuePair<string, string>> sequenceMap,
            IEnumerable<char> ignorableCharacters, IEnumerable<char> allowsEnglishDouble, IEnumerable<string> entries,
            bool expandEntryForms = true, IEnumerable<string>? allowedEntries = null
        ) {
            if (characterMap is null) { throw new ArgumentNullException(nameof(characterMap)); }
            if (sequenceMap is null) { throw new ArgumentNullException(nameof(sequenceMap)); }
            if (ignorableCharacters is null) { throw new ArgumentNullException(nameof(ignorableCharacters)); }
            if (allowsEnglishDouble is null) { throw new ArgumentNullException(nameof(allowsEnglishDouble)); }
            if (entries is null) { throw new ArgumentNullException(nameof(entries)); }

            Boundary = boundary;
            _characterMap = new Dictionary<char, string>(characterMap);
            Dictionary<string, string> sequenceMap1 = new Dictionary<string, string>(sequenceMap);
            _ignorableCharacters = new HashSet<char>(ignorableCharacters);
            _allowsDouble = new HashSet<char>(allowsEnglishDouble);
            _sequenceMappingsByFirstCharacter = BuildSequenceIndex(sequenceMap1);
            _matcher = new AhoCorasickMatcher(BuildPatterns(entries, allowedEntries, expandEntryForms), Boundary);
        }

        private HashSet<string> BuildPatterns(IEnumerable<string> entries, IEnumerable<string>? allowedEntries, bool expandEntryForms) {
            HashSet<string> patterns = expandEntryForms ? PopulateVariations(entries) : PopulateEncodedEntries(entries);
            if (allowedEntries is not null) { patterns.ExceptWith(PopulateEncodedEntries(allowedEntries)); }
            return patterns;
        }

        private HashSet<string> PopulateEncodedEntries(IEnumerable<string> entries) {
            HashSet<string> encodedEntries = new HashSet<string>();
            foreach (string entry in entries) {
                if (!string.IsNullOrEmpty(entry)) { PopulateEncodedWord(entry, encodedEntries); }
            }
            return encodedEntries;
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
