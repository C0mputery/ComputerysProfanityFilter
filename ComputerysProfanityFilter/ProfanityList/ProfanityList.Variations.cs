using System;
using System.Collections.Generic;

// FYI, I am very bad at English rules, a lot of this was derived from consulting LLMs for english rules.
namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private static readonly string[] RegularSuffixes = { "ed", "ing", "er" };

        private HashSet<string> GenerateEncodedVariations(IEnumerable<string> terms) {
            if (terms == null) { throw new ArgumentNullException(nameof(terms)); }

            HashSet<string> encodedForms = new HashSet<string>();
            foreach (string term in terms) {
                if (string.IsNullOrEmpty(term)) { continue; }
                if (StringHelper.ContainsWhitespace(term)) {
                    GenerateEncodedPhraseVariations(term, encodedForms);
                    continue;
                }

                GenerateEncodedWordVariations(term, encodedForms);
            }

            return encodedForms;
        }

        private void GenerateEncodedPhraseVariations(string phrase, HashSet<string> encodedForms) {
            string[] tokens = StringHelper.SplitOnWhitespace(phrase);
            int tokenCount = tokens.Length;
            HashSet<string>[] tokenForms = new HashSet<string>[tokenCount];
            for (int index = 0; index < tokenCount; index++) {
                tokenForms[index] = new HashSet<string>();
                AddWordForms(tokens[index], tokenForms[index]);
            }

            AddEncodedPhraseCombinations(tokenForms, new string[tokenCount], 0, encodedForms);
        }

        private void AddEncodedPhraseCombinations(HashSet<string>[] tokenForms, string[] selectedForms, int tokenIndex, HashSet<string> encodedForms) {
            if (tokenIndex == tokenForms.Length) {
                AddEncodedForm(string.Join(' ', selectedForms), encodedForms);
                return;
            }

            foreach (string form in tokenForms[tokenIndex]) {
                selectedForms[tokenIndex] = form;
                AddEncodedPhraseCombinations(tokenForms, selectedForms, tokenIndex + 1, encodedForms);
            }
        }

        private void GenerateEncodedWordVariations(string word, HashSet<string> encodedForms) {
            HashSet<string> wordForms = new HashSet<string>();
            AddWordForms(word, wordForms);
            foreach (string variation in wordForms) { AddEncodedForm(variation, encodedForms); }
        }

        private static void AddWordForms(string word, HashSet<string> forms) {
            forms.Add(word);

            // Skip on tiny tokens. noisy and pointless
            if (word.Length <= 2) { return; }

            string stem = word[..^1];
            char lastCharacter = word[^1];
            bool endsWithE = word.EndsWith("e", StringComparison.Ordinal);

            // ENGLISH RULE: Regular plurals usually take -s (cat -> cats).
            //               Words ending in s, x, z, ch, or sh usually take -es (watch -> watches).
            forms.Add(string.Concat(word, "s"));
            if (word.EndsWith("s", StringComparison.Ordinal) || word.EndsWith("x", StringComparison.Ordinal) ||
                word.EndsWith("z", StringComparison.Ordinal) || word.EndsWith("ch", StringComparison.Ordinal) ||
                word.EndsWith("sh", StringComparison.Ordinal)) {
                forms.Add(string.Concat(word, "es"));
            }

            // ENGLISH RULE: Pluralize `consonant + y` words by changing y to ies (cry -> cries).
            if (word.EndsWith("y", StringComparison.Ordinal) && StringHelper.IsConsonant(word[^2])) {
                forms.Add(string.Concat(stem, "ies"));

                // ENGLISH RULE: A final y after a consonant changes to i before -ed, -er, and -est (try -> tried, nasty -> nastier/nastiest).
                // Retain the broad forms added below too.
                forms.Add(string.Concat(stem, "ied"));
                forms.Add(string.Concat(stem, "ier"));
                forms.Add(string.Concat(stem, "iest"));
            }

            // ENGLISH RULE: Regular verb forms commonly use -ed, -ing, and -er (walk -> walked/walking/walker).
            //               Some consonant-vowel-consonant words double their final consonant before a suffix (run -> running, stop -> stopped).
            // This implementation is more broad than the rule and adds every suffix to all words,
            // while trying consonant doubling for every word that ends in a consonant.
            foreach (string suffix in RegularSuffixes) {
                forms.Add(string.Concat(word, suffix));
                if (StringHelper.IsConsonant(lastCharacter)) { forms.Add(word + lastCharacter + suffix); }
            }

            // ENGLISH RULE: A silent final -e is commonly dropped before -ing (hate -> hating),
            //               but is retained before -d or -r (hate -> hated, bake -> baker).
            // This implementation is more broad than the rule and tries both forms for all words ending in e.
            if (endsWithE) {
                forms.Add(string.Concat(stem, "ed"));
                forms.Add(string.Concat(stem, "ing"));
                forms.Add(string.Concat(stem, "er"));
            }

            // ENGLISH RULE: Final -ie changes to -y before -ing (die -> dying, lie -> lying).
            if (word.EndsWith("ie", StringComparison.Ordinal)) {
                forms.Add(string.Concat(word[..^2], "ying"));
            }

            // ENGLISH RULE: The suffix -ish can form adjectives (fool -> foolish, red -> reddish).
            //               Final -y is usually retained (baby -> babyish), while final -e spelling varies (whiteish/whitish, styleish/stylish).
            // This implementation is more broad than the rule and adds -ish to all words,
            // including consonant doubling and both final -e spellings.
            if (!word.EndsWith("ish", StringComparison.Ordinal)) {
                forms.Add(string.Concat(word, "ish"));
                if (StringHelper.EndsWithConsonantVowelConsonant(word)) { forms.Add(word + lastCharacter + "ish"); }
                if (endsWithE) { forms.Add(string.Concat(stem, "ish")); }
            }
        }

        private void AddEncodedForm(string form, HashSet<string> encodedForms) { encodedForms.Add(EncodeTerm(form)); }
    }
}
