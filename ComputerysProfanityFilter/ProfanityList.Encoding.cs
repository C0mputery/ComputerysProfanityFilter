using System;
using System.Collections.Generic;
using System.Text;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private static readonly EncodedText EmptyEncodedText = new EncodedText(string.Empty, Array.Empty<SourceSpan>());

        private EncodedText Encode(string value) {
            if (value is null) { throw new ArgumentNullException(nameof(value)); }

            int end = value.Length;
            while (end > 0 && _ignorableCharacters.Contains(value[end - 1])) { end--; }
            if (end == 0) { return EmptyEncodedText; }

            StringBuilder encoded = new StringBuilder(end);
            List<SourceSpan> sourceSpans = new List<SourceSpan>(end);

            encoded.Append(Boundary);
            sourceSpans.Add(new SourceSpan(0, 0));

            char previous = Boundary;
            int repetitions = 1;
            bool hasContent = false;
            for (int index = 0; index < end;) {
                int sourceStart = index;
                string? mapped = null;
                int mappedLength = 0;

                if (char.IsWhiteSpace(value[index]) || value[index] == Boundary) {
                    do { index++; }
                    while (index < end && (char.IsWhiteSpace(value[index]) || value[index] == Boundary));

                    if (previous == Boundary) {
                        int lastSpanIndex = sourceSpans.Count - 1;
                        SourceSpan previousSpan = sourceSpans[lastSpanIndex];
                        sourceSpans[lastSpanIndex] = new SourceSpan(previousSpan.Start, index);
                    }
                    else {
                        encoded.Append(Boundary);
                        sourceSpans.Add(new SourceSpan(sourceStart, index));
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
                        AppendEncodedCharacter(character, sourceStart, index, encoded, sourceSpans, ref previous, ref repetitions);
                        hasContent = true;
                        continue;
                    }
                }
                else { index += mappedLength; }

                foreach (char mappedCharacterValue in mapped!) {
                    AppendEncodedCharacter(mappedCharacterValue, sourceStart, index, encoded, sourceSpans, ref previous, ref repetitions);
                    hasContent = true;
                }
            }

            if (!hasContent) { return EmptyEncodedText; }
            if (previous != Boundary) {
                encoded.Append(Boundary);
                sourceSpans.Add(new SourceSpan(end, end));
            }
            return new EncodedText(encoded.ToString(), sourceSpans);
        }

        private void AppendEncodedCharacter(
            char value, int sourceStart, int sourceEnd, StringBuilder encoded, List<SourceSpan> sourceSpans,
            ref char previous, ref int repetitions
        ) {
            char mappedCharacter = char.ToLowerInvariant(value);
            if (mappedCharacter == previous) { repetitions++; }
            else {
                previous = mappedCharacter;
                repetitions = 1;
            }

            int maximumRepetitions = _allowsDouble.Contains(mappedCharacter) ? 2 : 1;
            if (repetitions <= maximumRepetitions) {
                encoded.Append(mappedCharacter);
                sourceSpans.Add(new SourceSpan(sourceStart, sourceEnd));
            }
            else if (sourceSpans.Count > 0) {
                int lastSpanIndex = sourceSpans.Count - 1;
                SourceSpan previousSpan = sourceSpans[lastSpanIndex];
                sourceSpans[lastSpanIndex] = new SourceSpan(previousSpan.Start, sourceEnd);
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
