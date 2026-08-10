using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private string Encode(string value) {
            if (value is null) { throw new ArgumentNullException(nameof(value)); }

            int end = value.Length;
            while (end > 0 && _ignorableCharacters.Contains(value[end - 1])) { end--; }
            if (end == 0) { return string.Empty; }

            StringBuilder encoded = new StringBuilder(end + 2);
            encoded.Append(Boundary);

            char previous = Boundary;
            int repetitions = 1;
            bool hasContent = false;
            for (int index = 0; index < end;) {
                string? mapped = null;
                int mappedLength = 0;

                if (char.IsWhiteSpace(value[index]) || value[index] == Boundary) {
                    do { index++; }
                    while (index < end && (char.IsWhiteSpace(value[index]) || value[index] == Boundary));

                    if (previous != Boundary) {
                        encoded.Append(Boundary);
                        previous = Boundary;
                        repetitions = 1;
                    }
                    continue;
                }

                char sequenceInitial = char.ToUpperInvariant(value[index]);
                if (_sequenceMappingsByFirstCharacter.TryGetValue(sequenceInitial, out SequenceMapping[]? candidates)) {
                    foreach (SequenceMapping sequence in candidates) {
                        if (index + sequence.Key.Length > end) { continue; }
                        if (string.Compare(value, index, sequence.Key, 0, sequence.Key.Length, StringComparison.OrdinalIgnoreCase) != 0) { continue; }

                        mapped = sequence.Value;
                        mappedLength = sequence.Key.Length;
                        break;
                    }
                }

                if (mappedLength == 0) {
                    char character = char.ToLowerInvariant(value[index]);
                    index++;

                    if (!_characterMap.TryGetValue(character, out mapped)) {
                        if (_ignorableCharacters.Contains(character)) { continue; }
                        AppendEncodedCharacter(character, encoded, ref previous, ref repetitions);
                        hasContent = true;
                        continue;
                    }
                }
                else { index += mappedLength; }

                foreach (char mappedCharacter in mapped!) {
                    AppendEncodedCharacter(mappedCharacter, encoded, ref previous, ref repetitions);
                    hasContent = true;
                }
            }

            if (!hasContent) { return string.Empty; }
            if (previous != Boundary) { encoded.Append(Boundary); }
            return encoded.ToString();
        }

        private void AppendEncodedCharacter(char value, StringBuilder encoded, ref char previous, ref int repetitions) {
            char mappedCharacter = char.ToLowerInvariant(value);
            if (mappedCharacter == previous) { repetitions++; }
            else {
                previous = mappedCharacter;
                repetitions = 1;
            }

            if (repetitions <= (_allowsDouble.Contains(mappedCharacter) ? 2 : 1)) {
                encoded.Append(mappedCharacter);
            }
        }

        public readonly struct EncodedText {
            public readonly string Value;
            public readonly IReadOnlyList<SourceSpan> SourceSpans;

            public EncodedText(string value, IReadOnlyList<SourceSpan> sourceSpans) {
                Value = value;
                SourceSpans = sourceSpans;
            }
        }

        public readonly struct SourceSpan {
            public readonly int Start;
            public readonly int End;

            public SourceSpan(int start, int end) {
                Start = start;
                End = end;
            }
        }

        private readonly struct SequenceMapping {
            public readonly string Key;
            public readonly string Value;

            public SequenceMapping(string key, string value) {
                Key = key;
                Value = value;
            }
        }
    }
}
