using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// FYI, I am very bad at English rules, a lot of this was derived from consulting LLMs for english rules.
namespace ComputerysProfanityFilter {
    public sealed partial class ProfanityList {
        private HashSet<string> PopulateVariations(IEnumerable<string> words) {
            if (words == null) { throw new ArgumentNullException(nameof(words)); }

            HashSet<string> forms = new HashSet<string>();
            foreach (string word in words) {
                if (string.IsNullOrEmpty(word)) { continue; }
                if (StringHelper.ContainsWhitespace(word)) {
                    PopulatePhraseVariations(word, forms);
                    continue;
                }

                PopulateWordVariations(word, forms);
            }

            return forms;
        }

        private void PopulatePhraseVariations(string phrase, HashSet<string> forms) {
            string[] tokens = StringHelper.SplitOnWhitespace(phrase);
            HashSet<string>[] tokenForms = new HashSet<string>[tokens.Length];
            for (int index = 0; index < tokens.Length; index++) {
                tokenForms[index] = new HashSet<string>();
                PopulateWordForms(tokens[index], tokenForms[index]);
            }

            PopulatePhraseCombinations(tokenForms, new string[tokens.Length], 0, forms);
        }

        private void PopulatePhraseCombinations(HashSet<string>[] tokenForms, string[] selectedForms, int tokenIndex, HashSet<string> forms) {
            if (tokenIndex == tokenForms.Length) {
                PopulateEncodedWord(string.Join(' ', selectedForms), forms);
                return;
            }

            foreach (string form in tokenForms[tokenIndex]) {
                selectedForms[tokenIndex] = form;
                PopulatePhraseCombinations(tokenForms, selectedForms, tokenIndex + 1, forms);
            }
        }

        private void PopulateWordVariations(string word, HashSet<string> forms) {
            HashSet<string> wordForms = new HashSet<string>();
            PopulateWordForms(word, wordForms);
            foreach (string variation in wordForms) {
                PopulateEncodedWord(variation, forms);
            }
        }

        private static void PopulateWordForms(string word, HashSet<string> forms) {
            forms.Add(word);

            // Skip on tiny tokens. noisy and pointless
            if (word.Length <= 2) { return; }

            string stem = word.Substring(0, word.Length - 1);
            char lastCharacter = word[^1];

            // ENGLISH RULE: Regular plurals usually take -s (cat -> cats).
            //               Words ending in s, x, z, ch, or sh usually take -es (watch -> watches).
            forms.Add(string.Concat(word, "s"));
            if (word.EndsWith("s", StringComparison.Ordinal) || word.EndsWith("x", StringComparison.Ordinal) ||
                word.EndsWith("z", StringComparison.Ordinal) || word.EndsWith("ch", StringComparison.Ordinal) ||
                word.EndsWith("sh", StringComparison.Ordinal)) {
                forms.Add(string.Concat(word, "es"));
            }

            // ENGLISH RULE: Pluralize `consonant + y` words by changing y to ies (cry -> cries).
            if (word.EndsWith("y", StringComparison.Ordinal) && IsConsonant(word[^2])) {
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
            foreach (string suffix in new[] { "ed", "ing", "er" }) {
                forms.Add(string.Concat(word, suffix));
                if (IsConsonant(lastCharacter)) { forms.Add(word + lastCharacter + suffix); }
            }

            // ENGLISH RULE: A silent final -e is commonly dropped before -ing (hate -> hating),
            //               but is retained before -d or -r (hate -> hated, bake -> baker).
            // This implementation is more broad than the rule and tries both forms for all words ending in e.
            if (word.EndsWith("e", StringComparison.Ordinal)) {
                forms.Add(string.Concat(stem, "ed"));
                forms.Add(string.Concat(stem, "ing"));
                forms.Add(string.Concat(stem, "er"));
            }

            // ENGLISH RULE: Final -ie changes to -y before -ing (die -> dying, lie -> lying).
            if (word.EndsWith("ie", StringComparison.Ordinal)) {
                forms.Add(string.Concat(word.Substring(0, word.Length - 2), "ying"));
            }

            // ENGLISH RULE: The suffix -ish can form adjectives (fool -> foolish, red -> reddish).
            //               Final -y is usually retained (baby -> babyish), while final -e spelling varies (whiteish/whitish, styleish/stylish).
            // This implementation is more broad than the rule and adds -ish to all words,
            // including consonant doubling and both final -e spellings.
            if (!word.EndsWith("ish", StringComparison.Ordinal)) {
                forms.Add(string.Concat(word, "ish"));
                if (EndsWithConsonantVowelConsonant(word)) { forms.Add(word + lastCharacter + "ish"); }
                if (word.EndsWith("e", StringComparison.Ordinal)) { forms.Add(string.Concat(stem, "ish")); }
            }

        }

        private static bool EndsWithConsonantVowelConsonant(string word) {
            if (word.Length < 3) { return false; }

            char lastCharacter = word[^1];
            return IsConsonant(word[^3]) && IsVowel(word[^2]) && IsConsonant(lastCharacter) && lastCharacter is not ('w' or 'x' or 'y');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsConsonant(char c) => c is >= 'a' and <= 'z' and not ('a' or 'e' or 'i' or 'o' or 'u');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u';

        private void PopulateEncodedWord(string form, HashSet<string> forms) {
            string encoded = Encode(form).Value;
            if (encoded.Length > 0) { forms.Add(encoded); }
        }

    }
}
