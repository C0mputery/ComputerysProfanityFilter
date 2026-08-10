# ComputerysProfanityFilter

A .NET Standard 2.1 (C# version 9 for unity) profanity filter that censors known words while recognizing common
obfuscations such as character substitutions, punctuation, repeated letters, and leetspeak.

With what I believe to be a reasonable enough default word list. [link here]

## Basic usage
Create one `ProfanityList` and reuse it when filtering messages. `Censor` returns the original
string unchanged when it finds no match.
```csharp
using ComputerysProfanityFilter;

ProfanityList profanityFilter = new ProfanityList();
string censored = profanityFilter.Censor("Please don't be an a$$hole.");
// censored: "Please don't be an #######."
```

Use the optional second argument to choose the replacement character:
```csharp
string censored = profanityFilter.Censor("Please don't be an a$$hole.", '*');
// censored: "Please don't be an *******."
```

Matching flexable, case-insensitive, it'll normalizes many substitutions (1337 speak, common enlgish rules),
ignores selected punctuation within words, and collapses excessive repeated letters.
Entire matches, including their intervening punctuation, are replaced with the censor character.

## Custom word list
Pass your own normalization rules and words to the constructor. By default, supplied words are
expanded into several common English forms, so use base words where appropriate. Set
`expandWordForms: false` to match only the supplied forms (after normalization).

```csharp
using System.Collections.Generic;
using ComputerysProfanityFilter;

ProfanityList filter = new ProfanityList(
    boundary: '\uFFFF',
    characterMap: new Dictionary<char, string> { ['4'] = "a", ['@'] = "a" },
    sequenceMap: new Dictionary<string, string>(),
    ignorableCharacters: new[] { '.', '-', '_' },
    allowsEnglishDouble: new[] { 'l', 's' },
    words: new[] { "spoiler", "example phrase" }
);

string censored = filter.Censor("S-p-o-i-l-e-r ahead");
// censored: "############# ahead"
```

`boundary` separates words and phrases internally. Choose a character that cannot/wont occur in normal
input; `\uFFFF` is the value used by the default filter. It is treated like whitespace if it is in the input.

For the precise normalization order and source-span behavior, see
[Encoding.md](ComputerysProfanityFilter/Encoding.md).

## Benchmarks

BenchmarkDotNet 0.15.8 results with .NET 10.0.10 on
an AMD Ryzen 7 5800X (Windows 11). The suite compares this library with
[`Profanity.Detector` 0.1.8](https://github.com/stephenhaunts/ProfanityDetector)
using equivalent vocabularies. Inputs are exactly 100, 1,000, 10,000, or
100,000 characters, made by repeating a fixed 10-message chat corpus containing
ordinary, profane, and obfuscated text.

### Censoring

| Engine and vocabulary | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter / expanded default (5,352 words) | 1.710 us, 0 B | 19.412 us, 4 KB | 192.328 us, 39 KB | 1.931 ms, 391 KB |
| Profanity.Detector / same expanded vocabulary (5,352 words) | 226.774 us, 5 KB | 1.567 ms, 165 KB | 15.379 ms, 13.7 MB | 231.053 ms, 1.32 GB |
| ComputerysProfanityFilter / Profanity.Detector raw vocabulary (1,626 words) | 2.254 us, 496 B | 21.094 us, 4 KB | 208.364 us, 39 KB | 1.953 ms, 392 KB |
| Profanity.Detector / raw default vocabulary (1,626 words) | 73.073 us, 5 KB | 562.448 us, 210 KB | 6.142 ms, 15.8 MB | 174.232 ms, 1.50 GB |

Each cell shows mean time followed by managed allocation per operation.
Across the measured input sizes, `Censor` was between 80x and 133x faster with the
expanded vocabulary, and between 27x and 89x faster with the raw vocabulary.

### Construction

Construction measurements create one filter instance.

| Engine and vocabulary | Mean | Allocated |
| --- | ---: | ---: |
| ComputerysProfanityFilter / expanded default (5,352 words) | 12.534 ms | 11.38 MB |
| Profanity.Detector / same expanded vocabulary (5,352 words) | 7.062 us | 54.69 KB |
| ComputerysProfanityFilter / Profanity.Detector raw vocabulary (1,626 words) | 1.328 ms | 2.76 MB |
| Profanity.Detector / raw default vocabulary (1,626 words) | 5.048 us | 25.58 KB |
