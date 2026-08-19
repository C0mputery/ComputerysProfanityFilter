using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ComputerysProfanityFilter {
    /// <summary>
    /// Provides the default profanity terms and character mappings.
    /// </summary>
    public static class DefaultProfanityList {
        /// <summary>
        /// The default profanity terms to detect.
        /// </summary>
        public static readonly IReadOnlyList<string> Terms = new ReadOnlyCollection<string>(new string[] {
            // swears
            "ass", "ahole", "asshole", "arsehole", "asshat", "asswipe",
            "bastard",
            "bitch", "biatch",
            "bullshit",
            "cock", "cocksucker",
            "cunt", "kunt",
            "dick", "dickhead",
            "dipshit",
            "douchebag",
            "dumbass", "dumbfuck",
            "fuck", "fcuk", "fuk", "fuq", "fux", "fux0r", "fvck", "fxck", "fucker", "fucking",
            "motherfucker",
            "piss",
            "shit", "shithead", "shitty",
            "slut",
            "twat",
            "wank",
            "wanker",
            "whore",

            // insults
            "assfuck", "assfucker",
            "bitchass",
            "buttfuck",
            "clusterfuck",
            "cockbite", "cockface", "cockfucker", "cockhead", "cockmunch", "cocknugget",
            "cumdumpster",
            "cuntface",
            "dickbag", "dickface", "dickwad", "dickweed",
            "dumbcunt", "dumbshit",
            "fatass",
            "fuckboy", "fuckface", "fuckhead", "fuckstick", "fucktard", "fuckwit",
            "jackass",
            "knobhead",
            "pigfucker",
            "shitbag", "shitcunt", "shitface", "shithole", "shitstain",
            "skullfuck",
            "slutbag",
            "son of a bitch",
            "twathead",
            "whoreface",

            // hate
            "baluga",
            "beaner",
            "chink",
            "cholo",
            "coon",
            "dago",
            "darkie",
            "dyke",
            "fag", "faggot",
            "gas the jews",
            "gayass", "gaybob", "gaydo", "gayfuck", "gayfuckist", "gaylord", "gaytard", "gaywad",
            "gook",
            "gypsy",
            "hitler",
            "homo",
            "kike",
            "kill all blacks", "kill all jews", "kill the blacks", "kill the jews",
            "lesbo",
            "mongoloid",
            "nazi", "nazism", "neonazi",
            "nigger", "nigga", "niga", "nigra", "niggle", "niglet", "nigress", "negro", "neger", "nig nog", "nig-nog", "nigaboo",
            "paki",
            "raghead",
            "rape",
            "retard", "reetard", "ritard", "r-tard", "tard",
            "sambo",
            "shemale",
            "sieg heil", "sig heil",
            "sissy",
            "spic",
            "towelhead",
            "tranny",
            "wetback",

            // self-harm
            "kill yourself", "kill your self", "kill you're self", "kys",

            // hate symbols
            "卐", "卍",
            "\u0fd5", "\u0fd6", "\u0fd7", "\u0fd8", // Google says they're swastika's, so we need to filter them.
            "ꖦ",
            "ᛋᛋ",
            "⚡⚡"
        });

        /// <summary>
        /// Terms that are exempt from censorship when they appear without repeated characters.
        /// </summary>
        public static readonly IReadOnlyList<string> AllowTerms = new ReadOnlyCollection<string>(new string[] {
            "as", // ass
            "con", // coon
            "go ok", // gook
            "a hole" // ahole
        });

        /// <summary>
        /// Characters, typically letters, that are expected to make up words.
        /// </summary>
        public static readonly IReadOnlyCollection<char> ExpectedCharacters = new ReadOnlyCollection<char>(new char[] {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        });

        /// <summary>
        /// Characters that split terms while preserving punctuation-obfuscation matching.
        /// </summary>
        public static readonly IReadOnlyCollection<char> BoundaryCharacters = new ReadOnlyCollection<char>(new char[] {
            '.', '_', '-', '*', '/', '\\', '~', '^', '=', '`', '\'', '"', ',', ';', ':', '?', '!', '#', '$',
            '%', '&', '+', '@', '|', '¿', '…', '–', '—', '•', '·', '(', ')', '[', ']', '{', '}', '<', '>',
            '‐', '‑', '‒', '―', '‽', '⁃', '‚', '„', '‟', '‘', '’', '“', '”', '‹', '›',
            '、', '。', '〃', '〈', '〉', '《', '》', '「', '」', '『', '』', '【', '】', '〔', '〕',
        });

        /// <summary>
        /// Maps individual characters to their replacement strings.
        /// </summary>
        public static readonly IReadOnlyDictionary<char, string> CharacterMap = new ReadOnlyDictionary<char, string>(CreateCharacterMap());
        private static Dictionary<char, string> CreateCharacterMap() {
            Dictionary<char, string> characterMap = new Dictionary<char, string> {
                // Single-character mappings, grouped by their mapped value.
                ['4'] = "a", ['@'] = "a", ['à'] = "a", ['á'] = "a", ['â'] = "a", ['ã'] = "a", ['ä'] = "a", ['å'] = "a", ['ā'] = "a", ['ă'] = "a", ['ą'] = "a", ['ǎ'] = "a", ['ǟ'] = "a", ['ǻ'] = "a", ['а'] = "a", ['α'] = "a",
                ['8'] = "b", ['ß'] = "b", ['в'] = "b", ['β'] = "b",
                ['<'] = "c", ['¢'] = "c", ['ç'] = "c", ['ć'] = "c", ['ĉ'] = "c", ['ċ'] = "c", ['č'] = "c", ['с'] = "c",
                ['ď'] = "d", ['đ'] = "d", ['ð'] = "d", ['ԁ'] = "d", ['δ'] = "d",
                ['3'] = "e", ['€'] = "e", ['è'] = "e", ['é'] = "e", ['ê'] = "e", ['ë'] = "e", ['ē'] = "e", ['ĕ'] = "e", ['ė'] = "e", ['ę'] = "e", ['ě'] = "e", ['е'] = "e", ['ε'] = "e",
                ['ƒ'] = "f",
                ['ĝ'] = "g", ['ğ'] = "g", ['ġ'] = "g", ['ģ'] = "g",
                ['#'] = "h", ['ĥ'] = "h", ['ħ'] = "h", ['һ'] = "h", ['н'] = "h",
                ['!'] = "i", ['1'] = "i", ['|'] = "i", ['¡'] = "i", ['ì'] = "i", ['í'] = "i", ['î'] = "i", ['ï'] = "i", ['ī'] = "i", ['ĭ'] = "i", ['į'] = "i", ['ı'] = "i", ['і'] = "i", ['ι'] = "i",
                ['ĵ'] = "j", ['ј'] = "j",
                ['ķ'] = "k", ['ĸ'] = "k", ['к'] = "k", ['κ'] = "k",
                ['ĺ'] = "l", ['ļ'] = "l", ['ľ'] = "l", ['ł'] = "l",
                ['м'] = "m",
                ['ñ'] = "n", ['ń'] = "n", ['ņ'] = "n", ['ň'] = "n", ['ŉ'] = "n", ['ŋ'] = "n", ['η'] = "n",
                ['0'] = "o", ['ò'] = "o", ['ó'] = "o", ['ô'] = "o", ['õ'] = "o", ['ö'] = "o", ['ø'] = "o", ['ō'] = "o", ['ŏ'] = "o", ['ő'] = "o", ['ơ'] = "o", ['о'] = "o", ['ο'] = "o",
                ['р'] = "p", ['ρ'] = "p",
                ['ԛ'] = "q",
                ['ŕ'] = "r", ['ŗ'] = "r", ['ř'] = "r",
                ['5'] = "s", ['$'] = "s", ['ś'] = "s", ['ŝ'] = "s", ['ş'] = "s", ['š'] = "s", ['ș'] = "s", ['ſ'] = "s", ['ѕ'] = "s",
                ['+'] = "t", ['7'] = "t", ['ţ'] = "t", ['ť'] = "t", ['ŧ'] = "t", ['ț'] = "t", ['т'] = "t", ['τ'] = "t",
                ['ù'] = "u", ['ú'] = "u", ['û'] = "u", ['ü'] = "u", ['ū'] = "u", ['ŭ'] = "u", ['ů'] = "u", ['ű'] = "u", ['ų'] = "u", ['ư'] = "u", ['υ'] = "u", ['μ'] = "u",
                ['ν'] = "v",
                ['ŵ'] = "w", ['ω'] = "w",
                ['х'] = "x", ['χ'] = "x",
                ['¥'] = "y", ['ý'] = "y", ['ÿ'] = "y", ['ŷ'] = "y", ['у'] = "y", ['γ'] = "y",
                ['2'] = "z", ['ź'] = "z", ['ż'] = "z", ['ž'] = "z",

                // Multi-character mappings, grouped by their mapped value.
                ['æ'] = "ae",
                ['ﬀ'] = "ff",
                ['ﬃ'] = "ffi",
                ['ﬄ'] = "ffl",
                ['ﬁ'] = "fi",
                ['ﬂ'] = "fl",
                ['œ'] = "oe",
                ['ﬅ'] = "ft",
                ['ﬆ'] = "st",
                ['þ'] = "th",
            };

            AddCompatibilityCharacterMappings(characterMap, '\uFF10', '\uFF19');
            AddCompatibilityCharacterMappings(characterMap, '\uFF21', '\uFF3A');
            AddCompatibilityCharacterMappings(characterMap, '\uFF41', '\uFF5A');
            AddCompatibilityCharacterMappings(characterMap, '\u00B2', '\u00B3');
            AddCompatibilityCharacterMappings(characterMap, '\u00B9', '\u00B9');
            AddCompatibilityCharacterMappings(characterMap, '\u2070', '\u209C');
            AddCompatibilityCharacterMappings(characterMap, '\u2460', '\u24FF');
            return characterMap;
        }
        private static void AddCompatibilityCharacterMappings(Dictionary<char, string> characterMap, char first, char last) {
            for (char character = first; character <= last; character++) {
                string mapped = GetCompatibilityMapping(character.ToString(), characterMap);
                if (mapped.Length > 0) { characterMap[character] = mapped; }
            }
        }

        /// <summary>
        /// Characters that are ignored within a term.
        /// </summary>
        public static readonly IReadOnlyCollection<char> JoinerCharacters = CreateJoinerCharacters();
        private static IReadOnlyCollection<char> CreateJoinerCharacters() {
            List<char> joinerCharacters = new List<char> {
                // Invisible
                '\u00AD', '\u034F', '\u061C', '\u180E', '\u200B', '\u200C', '\u200D', '\u200E', '\u200F',
                '\u2060', '\u2061', '\u2062', '\u2063', '\u2064', '\u2066', '\u2067', '\u2068', '\u2069', '\uFEFF',
            };

            AddCharacterRange(joinerCharacters, '\u0300', '\u036F');
            AddCharacterRange(joinerCharacters, '\uFE00', '\uFE0F');

            for (char character = '0'; character <= '9'; character++) {
                if (!CharacterMap.ContainsKey(character)) { joinerCharacters.Add(character); }
            }

            return new ReadOnlyCollection<char>(joinerCharacters);
        }
        private static void AddCharacterRange(ICollection<char> characters, char first, char last) {
            for (char character = first; character <= last; character++) {
                characters.Add(character);
            }
        }

        /// <summary>
        /// Maps character sequences to their replacement strings.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> SequenceMap = new ReadOnlyDictionary<string, string>(CreateSequenceMap());
        private static Dictionary<string, string> CreateSequenceMap() {
            Dictionary<string, string> sequenceMap = new Dictionary<string, string> {
                [@"/\/\"] = "m",
                [@"|\/|"] = "m",
                [@"\/\/"] = "w",
                [@"|/\|"] = "w",
                [@"|\|"] = "n",
                ["|/|"] = "n",
                [@"/\/"] = "n",
                ["|-|"] = "h",
                ["|_|"] = "u",
                [@"/\"] = "a",
                [@"\/"] = "v",
                ["|3"] = "b",
                ["|)"] = "d",
                ["|<"] = "k",
                ["|{"] = "k",
                ["><"] = "x",
                ["}{"] = "x",
            };

            for (int codePoint = 0x1D400; codePoint <= 0x1D7FF; codePoint++) {
                string mathematicalCharacter = char.ConvertFromUtf32(codePoint);
                string mapped = GetCompatibilityMapping(mathematicalCharacter, CharacterMap);
                if (mapped.Length > 0) { sequenceMap[mathematicalCharacter] = mapped; }
            }

            return sequenceMap;
        }
        private static string GetCompatibilityMapping(string value, IReadOnlyDictionary<char, string>? characterMap) {
            string normalized = value.Normalize(NormalizationForm.FormKC);
            StringBuilder mapped = new StringBuilder(normalized.Length);
            foreach (char character in normalized) {
                if ((character < 'A' || character > 'Z') && (character < 'a' || character > 'z') && (character < '0' || character > '9')) {
                    return string.Empty;
                }

                char lowerCharacter = char.ToLowerInvariant(character);
                if (characterMap != null && characterMap.TryGetValue(lowerCharacter, out string? mappedCharacter)) { mapped.Append(mappedCharacter); }
                else { mapped.Append(lowerCharacter); }
            }
            return mapped.ToString();
        }
    }
}
