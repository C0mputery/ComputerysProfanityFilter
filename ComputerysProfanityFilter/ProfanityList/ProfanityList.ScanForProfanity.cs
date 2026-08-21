using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private Node[] _nodes = null!;
        private Pattern[] _patterns = null!;
        private int _maximumPatternLength;

        private void InitializeAhoCorasick(IEnumerable<Pattern> patterns) {
            if (patterns == null) { throw new ArgumentNullException(nameof(patterns)); }

            List<NodeBuilder> nodes = new List<NodeBuilder> { new NodeBuilder() };
            List<Pattern> patternList = new List<Pattern>();
            Dictionary<string, int> patternIds = new Dictionary<string, int>(StringComparer.Ordinal);
            int maximumPatternLength = 0;

            foreach (Pattern pattern in patterns) { AddPattern(pattern); }
            Build();

            _nodes = Freeze();
            _patterns = patternList.ToArray();
            _maximumPatternLength = maximumPatternLength;

            return;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddPattern(Pattern pattern) {
                string encoded = pattern.Encoded;
                if (encoded.Length == 0) { throw new ArgumentException("Empty patterns are not supported.", nameof(patterns)); }

                if (patternIds.ContainsKey(encoded)) { return; }

                int patternId = patternList.Count;
                patternList.Add(pattern);
                patternIds.Add(encoded, patternId);
                maximumPatternLength = Math.Max(maximumPatternLength, encoded.Length);

                int nodeIndex = 0;
                foreach (char character in encoded) {
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

        private T ScanForProfanity<T>(string input, T handler) where T : struct, IMatchHandler {
            int nodeIndex = 0;
            Span<int> sourcePositionBuffer = stackalloc int[_maximumPatternLength]; // TODO: handle larger than expected maximum patterns
            SourcePositionWindow sourcePositions = new SourcePositionWindow(sourcePositionBuffer);
            bool hasPendingMatch = false;
            int pendingMatchStart = 0;
            int pendingMatchEnd = 0;
            Pattern? pendingPattern = null;
            bool mayStartPattern = true;
            char? previous = null;

            for (int textIndex = 0; textIndex < input.Length; textIndex++) {
                if (char.IsWhiteSpace(input[textIndex])) {
                    if (ResetAtBoundary()) { return handler; }
                    continue;
                }

                ReadOnlySpan<char> remaining = input.AsSpan(textIndex);
                bool hasSequenceMatch = _prefixMatcher.TryGetLongestMatch(
                    remaining,
                    out string? mapped,
                    out int mappedLength,
                    out bool alwaysCensor,
                    out char normalizedCharacter
                );
                if (alwaysCensor) {
                    if (CompletePendingMatch() || handler.HandleMatch(textIndex, textIndex + mappedLength - 1, mapped!)) {
                        return handler;
                    }

                    textIndex += mappedLength - 1;
                    continue;
                }

                if (hasSequenceMatch) {
                    if (_boundaryCharacters.Contains(input[textIndex]) && !CanDirectlyContinueCurrentPattern(mapped!)) {
                        if (ResetAtBoundary()) { return handler; }
                        textIndex += mappedLength - 1;
                        continue;
                    }

                    if (EncodeMappedCharacters(ref sourcePositions, mapped!, textIndex, textIndex + mappedLength - 1)) { return handler; }
                    textIndex += mappedLength - 1;
                    continue;
                }

                if (_characterMap.TryGetValue(normalizedCharacter, out mapped)) {
                    if (_boundaryCharacters.Contains(normalizedCharacter) && !CanDirectlyContinueCurrentPattern(mapped)) {
                        if (ResetAtBoundary()) { return handler; }
                        continue;
                    }

                    if (EncodeMappedCharacters(ref sourcePositions, mapped, textIndex, textIndex)) { return handler; }
                    continue;
                }

                if (_joinerCharacters.Contains(normalizedCharacter)) { continue; }

                if (_boundaryCharacters.Contains(normalizedCharacter)) {
                    if (ResetAtBoundary()) { return handler; }
                    continue;
                }

                if (!_expectedCharacters.Contains(normalizedCharacter)) {
                    if (ResetAtUnexpectedCharacter()) { return handler; }
                    continue;
                }

                if (AppendEncodedCharacter(ref sourcePositions, normalizedCharacter, textIndex, textIndex)) { return handler; }
            }
            CompletePendingMatch();

            return handler;

            bool EncodeMappedCharacters(ref SourcePositionWindow positions, string mapped, int start, int end) {
                foreach (char mappedCharacter in mapped) {
                    if (AppendEncodedCharacter(ref positions, mappedCharacter, start, end)) { return true; }
                }
                return false;
            }

            bool CanDirectlyContinueCurrentPattern(string mapped) {
                if (mapped.Length == 0 || (nodeIndex == 0 && !mayStartPattern)) { return false; }

                return _nodes[nodeIndex].Transitions.ContainsKey(mapped[0]);
            }

            bool ResetAtBoundary() {
                bool shouldStop = CompletePendingMatch();
                mayStartPattern = true;
                previous = null;
                return shouldStop;
            }

            bool ResetAtUnexpectedCharacter() {
                bool shouldStop = ResetAtBoundary();
                nodeIndex = 0;
                return shouldStop;
            }

            bool AppendEncodedCharacter(ref SourcePositionWindow positions, char character, int start, int end) {
                bool repeatsPrevious = character == previous;
                if (repeatsPrevious) {
                    ExtendPendingMatches(end);
                    return false;
                }

                previous = character;

                if (!mayStartPattern) { ClearPendingMatches(); }
                positions.Add(start);

                while (nodeIndex != 0 && !_nodes[nodeIndex].Transitions.ContainsKey(character)) { nodeIndex = _nodes[nodeIndex].FailureLink; }
                if (nodeIndex == 0 && !mayStartPattern) { return false; }
                if (_nodes[nodeIndex].Transitions.TryGetValue(character, out int nextNodeIndex)) { nodeIndex = nextNodeIndex; }

                foreach (int patternId in _nodes[nodeIndex].PatternIds) {
                    Pattern pattern = _patterns[patternId];
                    int matchStart = positions.FromEnd(pattern.Encoded.Length - 1);
                    if (!hasPendingMatch) {
                        hasPendingMatch = true;
                        pendingMatchStart = matchStart;
                    }
                    else { pendingMatchStart = Math.Min(pendingMatchStart, matchStart); }

                    pendingMatchEnd = end;
                    if (pendingPattern == null || pattern.IsPreferredTo(pendingPattern)) { pendingPattern = pattern; }
                }

                mayStartPattern = false;
                return false;
            }

            bool CompletePendingMatch() {
                if (!hasPendingMatch) { return false; }

                bool matched = !IsAllowedMatch();
                bool shouldStop = matched && handler.HandleMatch(pendingMatchStart, pendingMatchEnd, pendingPattern!.Term);
                ClearPendingMatches();
                return shouldStop;
            }

            bool IsAllowedMatch() {
                ReadOnlySpan<char> matchedText = input.AsSpan(pendingMatchStart, pendingMatchEnd - pendingMatchStart + 1);
                if (matchedText.Length > _longestTermLength) { return false; }

                foreach (string allowTerm in _allowTerms) {
                    if (matchedText.Equals(allowTerm.AsSpan(), StringComparison.InvariantCultureIgnoreCase)) { return true; }
                }
                return false;
            }

            void ExtendPendingMatches(int end) {
                if (hasPendingMatch) { pendingMatchEnd = end; }
            }

            void ClearPendingMatches() {
                hasPendingMatch = false;
                pendingPattern = null;
            }
        }

        private interface IMatchHandler {
            bool HandleMatch(int start, int end, string term);
        }
    }
}
