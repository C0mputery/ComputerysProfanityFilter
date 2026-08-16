using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private const int MinimumCensorBufferSize = 256;

        private Node[] _nodes = null!;
        private string[] _patterns = null!;
        private int _maximumPatternLength;

        private void InitializeAhoCorasick(IEnumerable<string> patterns) {
            if (patterns == null) { throw new ArgumentNullException(nameof(patterns)); }

            List<NodeBuilder> nodes = new List<NodeBuilder> { new NodeBuilder() };
            List<string> patternList = new List<string>();
            Dictionary<string, int> patternIds = new Dictionary<string, int>(StringComparer.Ordinal);
            int maximumPatternLength = 0;

            foreach (string pattern in patterns) { AddPattern(pattern); }
            Build();

            _nodes = Freeze();
            _patterns = patternList.ToArray();
            _maximumPatternLength = maximumPatternLength;

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddPattern(string pattern) {
                if (pattern == null) { throw new ArgumentNullException(nameof(pattern)); }
                if (pattern.Length == 0) { throw new ArgumentException("Empty patterns are not supported.", nameof(pattern)); }

                if (patternIds.ContainsKey(pattern)) { return; }

                int patternId = patternList.Count;
                patternList.Add(pattern);
                patternIds.Add(pattern, patternId);
                maximumPatternLength = Math.Max(maximumPatternLength, pattern.Length);

                int nodeIndex = 0;
                foreach (char character in pattern) {
                    if (!nodes[nodeIndex].Transitions.TryGetValue(character, out int nextNodeIndex)) {
                        nextNodeIndex = nodes.Count;
                        nodes[nodeIndex].Transitions.Add(character, nextNodeIndex);
                        nodes.Add(new NodeBuilder());
                    }

                    nodeIndex = nextNodeIndex;
                }

                nodes[nodeIndex].OwnPatternIds.Add(patternId);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Build() {
                Queue<int> pendingNodes = new Queue<int>();

                foreach (NodeBuilder node in nodes) {
                    node.PatternIds.Clear();
                    node.PatternIds.AddRange(node.OwnPatternIds);
                }

                foreach (int childNodeIndex in nodes[0].Transitions.Values) {
                    nodes[childNodeIndex].FailureLink = 0;
                    pendingNodes.Enqueue(childNodeIndex);
                }

                while (pendingNodes.Count > 0) {
                    int nodeIndex = pendingNodes.Dequeue();
                    NodeBuilder node = nodes[nodeIndex];

                    foreach ((char character, int childNodeIndex) in node.Transitions) {
                        int fallbackNodeIndex = node.FailureLink;

                        while (fallbackNodeIndex != 0 && !nodes[fallbackNodeIndex].Transitions.ContainsKey(character)) {
                            fallbackNodeIndex = nodes[fallbackNodeIndex].FailureLink;
                        }

                        nodes[childNodeIndex].FailureLink = nodes[fallbackNodeIndex].Transitions.GetValueOrDefault(character, 0);

                        nodes[childNodeIndex].PatternIds.AddRange(nodes[nodes[childNodeIndex].FailureLink].PatternIds);
                        pendingNodes.Enqueue(childNodeIndex);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Node[] Freeze() {
                Node[] frozenNodes = new Node[nodes.Count];
                for (int index = 0; index < frozenNodes.Length; index++) {
                    NodeBuilder builder = nodes[index];
                    frozenNodes[index] = new Node(builder.Transitions, builder.PatternIds.ToArray(), builder.FailureLink);
                }

                return frozenNodes;
            }
        }

        public string Censor(string input, char censorCharacter = '#') {
            if (input == null) { throw new ArgumentNullException(nameof(input)); }

            char[]? censoredCharacters = null; // TODO: use a span with a stack allocated backing given the length is shorter than it should be

            int nodeIndex = 0;
            Span<int> sourcePositionBuffer = stackalloc int[_maximumPatternLength]; // TODO: handle larger than expected maximum patterns
            SourcePositionWindow sourcePositions = new SourcePositionWindow(sourcePositionBuffer);
            bool hasPendingMatch = false;
            int pendingMatchStart = 0;
            int pendingMatchEnd = 0;
            bool mayStartPattern = true;
            char? previous = null;

            try {
                for (int textIndex = 0; textIndex < input.Length; textIndex++) {
                    if (char.IsWhiteSpace(input[textIndex])) {
                        ResetAtBoundary();
                        continue;
                    }

                    ReadOnlySpan<char> remaining = input.AsSpan(textIndex);
                    if (_sequenceMap.TryGetLongestMatch(remaining, out string? mapped, out int mappedLength)) {
                        if (_boundaryCharacters.Contains(input[textIndex]) && !CanDirectlyContinueCurrentPattern(mapped)) {
                            ResetAtBoundary();
                            textIndex += mappedLength - 1;
                            continue;
                        }

                        EncodeMappedCharacters(ref sourcePositions, mapped, textIndex, textIndex + mappedLength - 1);
                        textIndex += mappedLength - 1;
                        continue;
                    }

                    char character = char.ToLowerInvariant(input[textIndex]);
                    if (_characterMap.TryGetValue(character, out mapped)) {
                        if (_boundaryCharacters.Contains(character) && !CanDirectlyContinueCurrentPattern(mapped)) {
                            ResetAtBoundary();
                            continue;
                        }

                        EncodeMappedCharacters(ref sourcePositions, mapped, textIndex, textIndex);
                        continue;
                    }

                    if (_joinerCharacters.Contains(character)) { continue; }

                    if (_boundaryCharacters.Contains(character)) {
                        ResetAtBoundary();
                        continue;
                    }

                    if (!_expectedCharacters.Contains(character)) {
                        ResetAtUnexpectedCharacter();
                        continue;
                    }

                    AppendEncodedCharacter(ref sourcePositions, character, textIndex, textIndex);
                }

                CensorPendingMatches();

                return censoredCharacters == null ? input : new string(censoredCharacters, 0, input.Length);
            }
            finally {
                if (censoredCharacters != null) { ArrayPool<char>.Shared.Return(censoredCharacters); }
            }

            void EncodeMappedCharacters(ref SourcePositionWindow positions, string mapped, int start, int end) {
                foreach (char mappedCharacter in mapped) { AppendEncodedCharacter(ref positions, mappedCharacter, start, end); }
            }

            bool CanDirectlyContinueCurrentPattern(string mapped) {
                if (mapped.Length == 0 || (nodeIndex == 0 && !mayStartPattern)) { return false; }

                return _nodes[nodeIndex].Transitions.ContainsKey(mapped[0]);
            }

            void ResetAtBoundary() {
                CensorPendingMatches();
                mayStartPattern = true;
                previous = null;
            }

            void ResetAtUnexpectedCharacter() {
                ResetAtBoundary();
                nodeIndex = 0;
            }

            void AppendEncodedCharacter(ref SourcePositionWindow positions, char character, int start, int end) {
                bool repeatsPrevious = character == previous;
                if (repeatsPrevious) {
                    ExtendPendingMatches(end);
                    return;
                }

                previous = character;

                if (!mayStartPattern) { ClearPendingMatches(); }
                positions.Add(start);

                while (nodeIndex != 0 && !_nodes[nodeIndex].Transitions.ContainsKey(character)) { nodeIndex = _nodes[nodeIndex].FailureLink; }
                if (nodeIndex == 0 && !mayStartPattern) { return; }
                if (_nodes[nodeIndex].Transitions.TryGetValue(character, out int nextNodeIndex)) { nodeIndex = nextNodeIndex; }

                foreach (int patternId in _nodes[nodeIndex].PatternIds) {
                    string pattern = _patterns[patternId];
                    int matchStart = positions.FromEnd(pattern.Length - 1);
                    if (!hasPendingMatch) {
                        hasPendingMatch = true;
                        pendingMatchStart = matchStart;
                    }
                    else { pendingMatchStart = Math.Min(pendingMatchStart, matchStart); }

                    pendingMatchEnd = end;
                }

                mayStartPattern = false;
            }

            void CensorPendingMatches() {
                if (!hasPendingMatch) { return; }

                if (!IsAllowedMatch()) { CensorRange(pendingMatchStart, pendingMatchEnd); }
                ClearPendingMatches();
            }

            bool IsAllowedMatch() {
                ReadOnlySpan<char> matchedText = input.AsSpan(pendingMatchStart, pendingMatchEnd - pendingMatchStart + 1);
                if (matchedText.Length > _maximumAllowTermLength) { return false; }

                foreach (string allowTerm in _allowTerms) {
                    if (matchedText.Equals(allowTerm.AsSpan(), StringComparison.InvariantCultureIgnoreCase)) { return true; }
                }

                return false;
            }

            void ExtendPendingMatches(int end) {
                if (hasPendingMatch) { pendingMatchEnd = end; }
            }

            void ClearPendingMatches() { hasPendingMatch = false; }

            void CensorRange(int start, int end) {
                if (censoredCharacters == null) {
                    censoredCharacters = ArrayPool<char>.Shared.Rent(Math.Max(input.Length, MinimumCensorBufferSize));
                    input.CopyTo(0, censoredCharacters, 0, input.Length);
                }

                for (int index = start; index <= end; index++) { censoredCharacters[index] = censorCharacter; }
            }
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
    }
}
