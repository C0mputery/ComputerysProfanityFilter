using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private string EncodeTerm(ReadOnlySpan<char> term, bool collapseRepeatedCharacters = true) {
            int length = term.Length;
            StringBuilder encoded = new StringBuilder(length);

            char previous = '\0';

            for (int index = 0; index < length;) {
                if (char.IsWhiteSpace(term[index])) {
                    index++;
                    continue;
                }

                if (_sequenceMap.TryGetLongestMatch(term.Slice(index, length - index), out string? mapped, out int mappedLength)) {
                    index += mappedLength;
                    foreach (char mappedCharacter in mapped) { AppendEncodedTermCharacter(mappedCharacter); }
                }
                else {
                    char character = char.ToLowerInvariant(term[index]);
                    index++;

                    if (_characterMap.TryGetValue(character, out mapped)) {
                        AppendEncodedTermCharacters(mapped);
                        continue;
                    }

                    if (_joinerCharacters.Contains(character) || _boundaryCharacters.Contains(character)) { continue; }
                    AppendEncodedTermCharacter(character);
                }
            }

            return encoded.ToString();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AppendEncodedTermCharacters(string value) {
                foreach (char character in value) { AppendEncodedTermCharacter(character); }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AppendEncodedTermCharacter(char value) {
                if (collapseRepeatedCharacters && value == previous) { return; }

                previous = value;
                encoded.Append(value);
            }
        }
    }
}
