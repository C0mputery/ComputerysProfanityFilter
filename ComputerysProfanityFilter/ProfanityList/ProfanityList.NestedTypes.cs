using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        internal sealed class Pattern {
            internal Pattern(string encoded, string term, int termOrder) {
                Encoded = encoded;
                Term = term;
                TermOrder = termOrder;
            }

            internal string Encoded { get; }
            internal string Term { get; }
            private int TermOrder { get; }
            internal bool IsPreferredTo(Pattern other) => Term.Length > other.Term.Length || (Term.Length == other.Term.Length && TermOrder < other.TermOrder);
        }

        private ref struct SourcePositionWindow {
            private readonly Span<int> _positions;

            internal SourcePositionWindow(Span<int> positions) {
                _positions = positions;
                Count = 0;
            }

            private int Count { get; set; }

            internal void Add(int sourcePosition) {
                _positions[Count % _positions.Length] = sourcePosition;
                Count++;
            }

            internal int FromEnd(int offset) {
                int index = (Count - 1 - offset) % _positions.Length;
                return _positions[index];
            }
        }

        private sealed class Node {
            internal Node(Dictionary<char, int> transitions, int[] patternIds, int failureLink) {
                Transitions = transitions;
                PatternIds = patternIds;
                FailureLink = failureLink;
            }

            internal readonly Dictionary<char, int> Transitions;
            internal readonly int[] PatternIds;
            internal readonly int FailureLink;
        }

        private sealed class NodeBuilder {
            internal readonly Dictionary<char, int> Transitions = new Dictionary<char, int>();
            internal readonly List<int> OwnPatternIds = new List<int>();
            internal readonly List<int> PatternIds = new List<int>();
            internal int FailureLink;
        }

        private sealed class SequenceTrie {
            private readonly SequenceTrieNode _root = new SequenceTrieNode();

            internal void Add(string sequence, string mappedValue) {
                SequenceTrieNode sequenceTrieNode = _root;
                foreach (char character in sequence) {
                    char normalized = char.ToLowerInvariant(character);
                    if (!sequenceTrieNode.Children.TryGetValue(normalized, out SequenceTrieNode? child)) {
                        child = new SequenceTrieNode();
                        sequenceTrieNode.Children.Add(normalized, child);
                    }

                    sequenceTrieNode = child;
                }

                sequenceTrieNode.MappedValue = mappedValue;
            }

            internal bool TryGetLongestMatch(ReadOnlySpan<char> value, [NotNullWhen(true)] out string? mappedValue, out int matchedLength) {
                SequenceTrieNode sequenceTrieNode = _root;
                mappedValue = null;
                matchedLength = 0;

                for (int index = 0; index < value.Length; index++) {
                    char normalized = char.ToLowerInvariant(value[index]);
                    if (!sequenceTrieNode.Children.TryGetValue(normalized, out sequenceTrieNode)) { break; }
                    if (sequenceTrieNode.MappedValue == null) { continue; }

                    mappedValue = sequenceTrieNode.MappedValue;
                    matchedLength = index + 1;
                    if (sequenceTrieNode.Children.Count == 0) { return true; }
                }

                return matchedLength != 0;
            }

            private sealed class SequenceTrieNode {
                internal readonly Dictionary<char, SequenceTrieNode> Children = new Dictionary<char, SequenceTrieNode>();
                internal string? MappedValue;
            }
        }
    }
}
