using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        public string Censor(string input, char censorChar = '#') {
            if (input is null) { throw new ArgumentNullException(nameof(input)); }
            if (input.Length == 0) { return input; }

            int end = input.Length;
            while (end > 0 && _ignorableCharacters.Contains(input[end - 1])) { end--; }
            if (end == 0) { return input; }

            SourceSpan[] sourceSpans = ArrayPool<SourceSpan>.Shared.Rent(Math.Max(16, end));
            int state = 0;
            int encodedIndex = 0;
            sourceSpans[encodedIndex] = new SourceSpan(0, 0);
            _matcher.AdvanceHasMatch(ref state, Boundary);

            char previous = Boundary;
            int repetitions = 1;
            int tokenLength = 0;
            bool skippingOverlongToken = false;
            bool previousEndsMatch = false;
            StringBuilder? result = null;

            try {
                for (int index = 0; index < end;) {
                    int sourceStart = index;
                    string? mapped = null;
                    int mappedLength = 0;

                    if (skippingOverlongToken && !char.IsWhiteSpace(input[index]) && input[index] != Boundary) {
                        index++;
                        continue;
                    }

                    if (char.IsWhiteSpace(input[index]) || input[index] == Boundary) {
                        do { index++; }
                        while (index < end && (char.IsWhiteSpace(input[index]) || input[index] == Boundary));

                        if (previous == Boundary) {
                            SourceSpan previousSpan = sourceSpans[encodedIndex];
                            sourceSpans[encodedIndex] = new SourceSpan(previousSpan.Start, index);
                        }
                        else {
                            EnsureSpanCapacity(ref sourceSpans, encodedIndex + 2);
                            sourceSpans[++encodedIndex] = new SourceSpan(sourceStart, index);
                            previousEndsMatch = _matcher.AdvanceHasMatch(ref state, Boundary) &&
                                                _matcher.CensorMatchesEndingAt(
                                                    state, encodedIndex, sourceSpans, input, censorChar, ref result
                                                );
                            previous = Boundary;
                            repetitions = 1;
                        }
                        tokenLength = 0;
                        skippingOverlongToken = false;
                        continue;
                    }

                    char sequenceInitial = char.ToUpperInvariant(input[index]);
                    if (_sequenceMappingsByFirstCharacter.TryGetValue(sequenceInitial, out SequenceMapping[]? candidates)) {
                        foreach (SequenceMapping sequence in candidates) {
                            if (index + sequence.Key.Length > end) { continue; }
                            if (string.Compare(input, index, sequence.Key, 0, sequence.Key.Length, StringComparison.OrdinalIgnoreCase) != 0) { continue; }

                            mapped = sequence.Value;
                            mappedLength = sequence.Key.Length;
                            break;
                        }
                    }

                    if (mappedLength == 0) {
                        char character = char.ToLowerInvariant(input[index]);
                        index++;

                        if (!_characterMap.TryGetValue(character, out mapped)) {
                            if (_ignorableCharacters.Contains(character)) { continue; }
                            if (character == previous) { repetitions++; }
                            else {
                                previous = character;
                                repetitions = 1;
                            }

                            if (repetitions > (_allowsDouble.Contains(character) ? 2 : 1)) {
                                if (previousEndsMatch) {
                                    SourceSpan previousSpan = sourceSpans[encodedIndex];
                                    result ??= new StringBuilder(input);
                                    for (int sourceIndex = previousSpan.End; sourceIndex < index; sourceIndex++) {
                                        result[sourceIndex] = censorChar;
                                    }
                                    sourceSpans[encodedIndex] = new SourceSpan(previousSpan.Start, index);
                                }
                                else if (encodedIndex >= 0) {
                                    SourceSpan previousSpan = sourceSpans[encodedIndex];
                                    sourceSpans[encodedIndex] = new SourceSpan(previousSpan.Start, index);
                                }
                                continue;
                            }

                            tokenLength++;
                            if (tokenLength > _matcher.MaximumTokenLength) {
                                state = 0;
                                previousEndsMatch = false;
                                skippingOverlongToken = true;
                                continue;
                            }

                            EnsureSpanCapacity(ref sourceSpans, encodedIndex + 2);
                            sourceSpans[++encodedIndex] = new SourceSpan(sourceStart, index);
                            previousEndsMatch = _matcher.AdvanceHasMatch(ref state, character) &&
                                                _matcher.CensorMatchesEndingAt(
                                                    state, encodedIndex, sourceSpans, input, censorChar, ref result
                                                );
                            continue;
                        }
                    }
                    else { index += mappedLength; }

                    foreach (char mappedCharacterValue in mapped!) {
                        char mappedCharacter = char.ToLowerInvariant(mappedCharacterValue);
                        if (mappedCharacter == previous) { repetitions++; }
                        else {
                            previous = mappedCharacter;
                            repetitions = 1;
                        }

                        if (repetitions > (_allowsDouble.Contains(mappedCharacter) ? 2 : 1)) {
                            if (encodedIndex >= 0) {
                                SourceSpan previousSpan = sourceSpans[encodedIndex];
                                if (previousEndsMatch) {
                                    result ??= new StringBuilder(input);
                                    for (int sourceIndex = previousSpan.End; sourceIndex < index; sourceIndex++) {
                                        result[sourceIndex] = censorChar;
                                    }
                                }
                                sourceSpans[encodedIndex] = new SourceSpan(previousSpan.Start, index);
                            }
                            continue;
                        }

                        tokenLength++;
                        if (tokenLength > _matcher.MaximumTokenLength) {
                            state = 0;
                            previousEndsMatch = false;
                            skippingOverlongToken = true;
                            break;
                        }

                        EnsureSpanCapacity(ref sourceSpans, encodedIndex + 2);
                        sourceSpans[++encodedIndex] = new SourceSpan(sourceStart, index);
                        previousEndsMatch = _matcher.AdvanceHasMatch(ref state, mappedCharacter) &&
                                            _matcher.CensorMatchesEndingAt(
                                                state, encodedIndex, sourceSpans, input, censorChar, ref result
                                            );
                    }
                }

                if (previous != Boundary) {
                    EnsureSpanCapacity(ref sourceSpans, encodedIndex + 2);
                    sourceSpans[++encodedIndex] = new SourceSpan(end, end);
                    _matcher.AdvanceHasMatch(ref state, Boundary);
                    _matcher.CensorMatchesEndingAt(
                        state, encodedIndex, sourceSpans, input, censorChar, ref result
                    );
                }

                return result?.ToString() ?? input;
            }
            finally { ArrayPool<SourceSpan>.Shared.Return(sourceSpans); }
        }

        private static void EnsureSpanCapacity(ref SourceSpan[] sourceSpans, int requiredLength) {
            if (requiredLength <= sourceSpans.Length) { return; }

            SourceSpan[] expanded = ArrayPool<SourceSpan>.Shared.Rent(Math.Max(requiredLength, sourceSpans.Length * 2));
            Array.Copy(sourceSpans, expanded, sourceSpans.Length);
            ArrayPool<SourceSpan>.Shared.Return(sourceSpans);
            sourceSpans = expanded;
        }

        private sealed class AhoCorasickMatcher {
            private readonly List<Node> _nodes = new List<Node> { new Node() };
            private readonly char _boundary;

            public int MaximumTokenLength { get; private set; }

            public AhoCorasickMatcher(IEnumerable<string> patterns, char boundary) {
                _boundary = boundary;
                foreach (string pattern in patterns) { Add(pattern); }
                BuildFailureLinks();
            }

            public bool CensorMatchesEndingAt(
                int state, int index, SourceSpan[] sourceSpans, string input, char censorChar,
                ref StringBuilder? result
            ) {
                if (_nodes[state].MatchLengths.Count == 0) { return false; }

                result ??= new StringBuilder(input);
                foreach (int length in _nodes[state].MatchLengths) {
                    int sourceStart = sourceSpans[index - length + 2].Start;
                    int sourceEnd = sourceSpans[index - 1].End;
                    for (int sourceIndex = sourceStart; sourceIndex < sourceEnd; sourceIndex++) {
                        result[sourceIndex] = censorChar;
                    }
                }
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AdvanceHasMatch(ref int state, char character) {
                while (state != 0 && !_nodes[state].Transitions.ContainsKey(character)) { state = _nodes[state].Failure; }
                if (_nodes[state].Transitions.TryGetValue(character, out int next)) { state = next; }
                return _nodes[state].MatchLengths.Count != 0;
            }

            private void Add(string pattern) {
                int state = 0;
                int tokenLength = 0;
                foreach (char character in pattern) {
                    if (!_nodes[state].Transitions.TryGetValue(character, out int next)) {
                        next = _nodes.Count;
                        _nodes[state].Transitions.Add(character, next);
                        _nodes.Add(new Node());
                    }
                    state = next;

                    if (character == _boundary) {
                        if (tokenLength > MaximumTokenLength) { MaximumTokenLength = tokenLength; }
                        tokenLength = 0;
                    }
                    else { tokenLength++; }
                }
                _nodes[state].MatchLengths.Add(pattern.Length);
            }

            private void BuildFailureLinks() {
                Queue<int> queue = new Queue<int>();
                foreach (int child in _nodes[0].Transitions.Values) { queue.Enqueue(child); }

                while (queue.Count > 0) {
                    int state = queue.Dequeue();
                    foreach ((char character, int child) in _nodes[state].Transitions) {
                        int failure = _nodes[state].Failure;
                        while (failure != 0 && !_nodes[failure].Transitions.ContainsKey(character)) { failure = _nodes[failure].Failure; }
                        if (_nodes[failure].Transitions.TryGetValue(character, out int fallback)) { _nodes[child].Failure = fallback; }
                        _nodes[child].MatchLengths.AddRange(_nodes[_nodes[child].Failure].MatchLengths);
                        queue.Enqueue(child);
                    }
                }
            }

            private sealed class Node {
                public readonly Dictionary<char, int> Transitions = new Dictionary<char, int>();
                public readonly List<int> MatchLengths = new List<int>();
                public int Failure;
            }
        }
    }
}
