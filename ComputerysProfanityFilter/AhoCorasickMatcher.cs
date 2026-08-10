using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ComputerysProfanityFilter {
    internal sealed class AhoCorasickMatcher {
            private readonly List<Node> _nodes = new List<Node> { new Node() };
            private readonly char _boundary;

            public int MaximumTokenLength { get; private set; }

            public AhoCorasickMatcher(IEnumerable<string> patterns, char boundary) {
                _boundary = boundary;
                foreach (string pattern in patterns) { Add(pattern); }
                BuildFailureLinks();
            }

            public bool CensorMatchesEndingAt(int state, int index, ProfanityList.SourceSpan[] sourceSpans, string input, char censorChar, ref StringBuilder? result) {
                List<int>? matchLengths = _nodes[state].MatchLengths;
                if (matchLengths is null) { return false; }

                result ??= new StringBuilder(input);
                foreach (int length in matchLengths) {
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
                while (state != 0) {
                    Dictionary<char, int>? transitions = _nodes[state].Transitions;
                    if (transitions is not null && transitions.TryGetValue(character, out int nextState)) {
                        state = nextState;
                        return _nodes[state].MatchLengths is not null;
                    }
                    state = _nodes[state].Failure;
                }

                Dictionary<char, int>? rootTransitions = _nodes[0].Transitions;
                if (rootTransitions is not null && rootTransitions.TryGetValue(character, out int next)) { state = next; }
                return _nodes[state].MatchLengths is not null;
            }

            private void Add(string pattern) {
                int state = 0;
                int tokenLength = 0;
                foreach (char character in pattern) {
                    Dictionary<char, int>? transitions = _nodes[state].Transitions;
                    if (transitions is null) {
                        transitions = new Dictionary<char, int>();
                        _nodes[state].Transitions = transitions;
                    }
                    if (!transitions.TryGetValue(character, out int next)) {
                        next = _nodes.Count;
                        transitions.Add(character, next);
                        _nodes.Add(new Node());
                    }
                    state = next;

                    if (character == _boundary) {
                        if (tokenLength > MaximumTokenLength) { MaximumTokenLength = tokenLength; }
                        tokenLength = 0;
                    }
                    else { tokenLength++; }
                }
                (_nodes[state].MatchLengths ??= new List<int>(1)).Add(pattern.Length);
            }

            private void BuildFailureLinks() {
                Queue<int> queue = new Queue<int>();
                Dictionary<char, int>? rootTransitions = _nodes[0].Transitions;
                if (rootTransitions is null) { return; }
                foreach (int child in rootTransitions.Values) { queue.Enqueue(child); }

                while (queue.Count > 0) {
                    int state = queue.Dequeue();
                    Dictionary<char, int>? transitions = _nodes[state].Transitions;
                    if (transitions is null) { continue; }
                    foreach ((char character, int child) in transitions) {
                        int failure = _nodes[state].Failure;
                        while (failure != 0 && (_nodes[failure].Transitions is null || !_nodes[failure].Transitions!.ContainsKey(character))) {
                            failure = _nodes[failure].Failure;
                        }
                        Dictionary<char, int>? fallbackTransitions = _nodes[failure].Transitions;
                        if (fallbackTransitions is not null && fallbackTransitions.TryGetValue(character, out int fallback)) {
                            _nodes[child].Failure = fallback;
                        }

                        List<int>? inheritedMatches = _nodes[_nodes[child].Failure].MatchLengths;
                        if (inheritedMatches is not null) {
                            (_nodes[child].MatchLengths ??= new List<int>(inheritedMatches.Count)).AddRange(inheritedMatches);
                        }
                        queue.Enqueue(child);
                    }
                }
            }

            private sealed class Node {
                public Dictionary<char, int>? Transitions;
                public List<int>? MatchLengths;
                public int Failure;
            }
    }
}
