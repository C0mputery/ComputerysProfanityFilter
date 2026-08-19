namespace ComputerysProfanityFilter {
    /// <summary>
    /// Describes a profanity match found in input text.
    /// </summary>
    public sealed class ProfanityMatch {
        /// <summary>
        /// The zero-based inclusive start index of the match in the input text.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// The zero-based inclusive end index of the match in the input text.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// The configured term that matched the input text.
        /// </summary>
        public string Term { get; }

        /// <summary>
        /// Creates a profanity match for a span of input text.
        /// </summary>
        /// <param name="start">The zero-based inclusive start index of the match.</param>
        /// <param name="end">The zero-based inclusive end index of the match.</param>
        /// <param name="term">The configured term that matched the input text.</param>
        public ProfanityMatch(int start, int end, string term) {
            Start = start;
            End = end;
            Term = term;
        }
    }
}
