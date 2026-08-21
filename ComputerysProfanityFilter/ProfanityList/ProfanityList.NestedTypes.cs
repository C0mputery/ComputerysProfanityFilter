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

        private sealed class PrefixMatcher {
            private const int BinarySearchThreshold = 8;

            private readonly Entry[] _entries;
            private readonly int[] _entryStart;
            private readonly int[] _entryCount;
            private readonly string?[] _matchValues;
            private readonly bool[] _alwaysCensor;

            internal PrefixMatcher(IEnumerable<KeyValuePair<string, string>> sequenceMap, IEnumerable<string> alwaysCensorTerms) {
                MutableNode root = new MutableNode();
                foreach ((string sequence, string matchValue) in sequenceMap) {
                    Add(root, sequence, CollapseRepeatedCharacters(matchValue), false);
                }

                HashSet<string> seenAlwaysCensorTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string term in alwaysCensorTerms) {
                    if (term.Length > 0 && char.IsWhiteSpace(term[0])) {
                        throw new ArgumentException("Always censor terms must not start with whitespace.", nameof(alwaysCensorTerms));
                    }

                    if (!seenAlwaysCensorTerms.Add(term)) { continue; }
                    if (term.Length == 0) { continue; }

                    Add(root, term, term, true);
                }

                List<MutableNode> order = new List<MutableNode> { root };
                Dictionary<MutableNode, int> indices = new Dictionary<MutableNode, int> { [root] = 0 };
                for (int index = 0; index < order.Count; index++) {
                    foreach (MutableNode child in order[index].Children.Values) {
                        indices.Add(child, order.Count);
                        order.Add(child);
                    }
                }

                int nodeCount = order.Count;
                List<Entry> entries = new List<Entry>();
                _entryStart = new int[nodeCount];
                _entryCount = new int[nodeCount];
                _matchValues = new string?[nodeCount];
                _alwaysCensor = new bool[nodeCount];

                for (int index = 0; index < nodeCount; index++) {
                    MutableNode node = order[index];
                    _entryStart[index] = entries.Count;
                    _entryCount[index] = node.Children.Count;

                    char[] nodeKeys = new char[node.Children.Count];
                    node.Children.Keys.CopyTo(nodeKeys, 0);
                    Array.Sort(nodeKeys);
                    foreach (char key in nodeKeys) {
                        entries.Add(new Entry(key, indices[node.Children[key]]));
                    }

                    _matchValues[index] = node.MatchValue;
                    _alwaysCensor[index] = node.AlwaysCensor;
                }

                _entries = entries.ToArray();
            }

            private static void Add(MutableNode root, string sequence, string matchValue, bool alwaysCensor) {
                MutableNode mutableNode = root;
                foreach (char character in sequence) {
                    char normalized = char.ToLowerInvariant(character);
                    if (!mutableNode.Children.TryGetValue(normalized, out MutableNode? child)) {
                        child = new MutableNode();
                        mutableNode.Children.Add(normalized, child);
                    }

                    mutableNode = child;
                }

                mutableNode.MatchValue = matchValue;
                mutableNode.AlwaysCensor = alwaysCensor;
            }

            // TODO:
            // TryGetLongestMatch is called once per candidate position with overlapping windows,
            // so every char in a match region gets run through char.ToLowerInvariant possibly up to longest sequence length times * 2.
            // Tested one one promising fix:
            // - Normalize the input once into a stack allocated lookahead window as long as the longest sequence length
            //   then shift + fill tail as the scan advances and match over pre-normalized data.
            //   Break-even around longest sequence length 8, wins ~2x at 16.
            //   for lookahead lengths below ~8 the shift bookkeeping cost more than it saved, leaving this code as-is is faster.
            //   Implement only if sequences longer than ~8 characters are expected (or figure out a happy medium)
            internal bool TryGetLongestMatch(
                ReadOnlySpan<char> value,
                [NotNullWhen(true)] out string? matchValue,
                out int matchedLength,
                out bool alwaysCensor,
                out char firstNormalized
            ) {
                int node = 0;
                matchValue = null;
                matchedLength = 0;
                alwaysCensor = false;
                firstNormalized = char.ToLowerInvariant(value[0]);

                for (int index = 0; index < value.Length; index++) {
                    char normalized = index == 0 ? firstNormalized : char.ToLowerInvariant(value[index]);
                    int start = _entryStart[node];
                    int count = _entryCount[node];
                    int next;
                    if (count > BinarySearchThreshold) { next = FindChild(start, count, normalized); }
                    else {
                        next = -1;
                        for (int entryIndex = start; entryIndex < start + count; entryIndex++) {
                            if (_entries[entryIndex].Key != normalized) { continue; }

                            next = _entries[entryIndex].Node;
                            break;
                        }
                    }

                    if (next < 0) { break; }
                    node = next;
                    string? candidate = _matchValues[node];
                    if (candidate == null) { continue; }

                    matchValue = candidate;
                    matchedLength = index + 1;
                    alwaysCensor = _alwaysCensor[node];
                    if (_entryCount[node] == 0) { return true; }
                }

                return matchedLength != 0;
            }

            private int FindChild(int start, int count, char normalized) {
                int low = 0;
                int high = count - 1;
                while (low <= high) {
                    int middle = low + ((high - low) >> 1);
                    Entry entry = _entries[start + middle];
                    if (entry.Key == normalized) { return entry.Node; }
                    if (entry.Key < normalized) { low = middle + 1; }
                    else { high = middle - 1; }
                }

                return -1;
            }

            private sealed class MutableNode {
                internal readonly Dictionary<char, MutableNode> Children = new Dictionary<char, MutableNode>();
                internal string? MatchValue;
                internal bool AlwaysCensor;
            }

            private readonly struct Entry {
                internal Entry(char key, int node) {
                    Key = key;
                    Node = node;
                }

                internal char Key { get; }
                internal int Node { get; }
            }
        }
    }
}
