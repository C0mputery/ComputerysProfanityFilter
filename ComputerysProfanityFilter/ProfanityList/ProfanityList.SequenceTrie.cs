using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
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
