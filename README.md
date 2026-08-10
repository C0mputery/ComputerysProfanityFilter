# ComputerysProfanityFilter

A .NET Standard 2.1 (C# version 9 for unity) profanity filter that censors known words/phrases while recognizing common
obfuscations such as character substitutions, punctuation, repeated letters, and leetspeak.
The [default word/phrase list](ComputerysProfanityFilter/DefaultProfanityList.cs) is resonable and covers swares, hate speach, and self harm.

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

## Custom word/phrase list
Pass your own normalization rules and words/phrases to the constructor. By default, supplied words/phrases
are expanded into several common English forms, so use base words where appropriate. Set
`expandWordForms: false` to match only the supplied forms (after normalization).
`allowedWords` removes matching normalized entries without expanding word forms.

```csharp
using System.Collections.Generic;
using ComputerysProfanityFilter;

ProfanityList filter = new ProfanityList(
    boundary: '\uFFFF',
    characterMap: new Dictionary<char, string> { ['4'] = "a", ['@'] = "a" },
    sequenceMap: new Dictionary<string, string>(),
    ignorableCharacters: new[] { '.', '-', '_' },
    allowsEnglishDouble: new[] { 'l', 's' },
    words: new[] { "spoiler", "example phrase" },
    allowedWords: new[] { "example phrase" }
);

string censored = filter.Censor("S-p-o-i-l-e-r ahead");
// censored: "############# ahead"
```

## Default configuration

`DefaultProfanityList` exposes the built-in settings as read-only collections for reuse in custom filters.

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

Results are grouped by vocabulary so each comparison uses the same word list.
Times are mean time per `Censor` operation; allocation is managed allocation per operation.

#### Expanded default vocabulary (5,352 words)
| Mean Time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 1.658 us | 17.570 us | 174.782 us | 1.677 ms |
| Profanity.Detector | 226.973 us | 1.532 ms | 15.114 ms | 224.520 ms |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 0 B | 4 KB | 39 KB | 391 KB |
| Profanity.Detector | 5 KB | 165 KB | 13.7 MB | 1.32 GB |

Across the measured input sizes, `Censor` was between 86x and 137x faster with the
expanded vocabulary.

#### Profanity.Detector raw vocabulary (1,626 words)
| Mean time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 1.802 us | 18.823 us | 198.936 us | 1.792 ms |
| Profanity.Detector | 73.572 us | 541.266 us | 5.453 ms | 145.947 ms |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 496 B | 4 KB | 39 KB | 392 KB |
| Profanity.Detector | 5 KB | 210 KB | 15.8 MB | 1.50 GB |

Across the measured input sizes, `Censor` was between 27x and 81x faster with the
raw vocabulary.

### Construction

Construction measurements create one filter instance. This system doesn't have free construction, so be a little bit wary of that. Make sure that you cache your instance after it's created.

| Engine and vocabulary | Mean | Allocated |
| --- | ---: | ---: |
| ComputerysProfanityFilter / expanded default (5,352 words) | 6.882 ms | 6.14 MB |
| Profanity.Detector / same expanded vocabulary (5,352 words) | 5.858 us | 54.69 KB |
| ComputerysProfanityFilter / Profanity.Detector raw vocabulary (1,626 words) | 881.638 us | 1.93 MB |
| Profanity.Detector / raw default vocabulary (1,626 words) | 4.637 us | 25.58 KB |
