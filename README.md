# ComputerysProfanityFilter

A .NET Standard 2.1 (C# version 9 for unity) profanity filter that censors known terms while recognizing common
obfuscations such as character substitutions, punctuation, inserted whitespace, repeated letters, and leetspeak.
The [default term list](ComputerysProfanityFilter/DefaultProfanityList.cs) is reasonable and covers swears, hate speech, and self-harm.

This library is currently used in [STRAFTAT](https://store.steampowered.com/app/2386720/STRAFTAT/) (as of the 1.4.9 update) to filter text chat, player names, lobby names, etc, albeit with a slightly cut down word list (swears removed).

## Installation
### NuGet
Install the package with the .NET CLI:
```
dotnet add package ComputerysProfanityFilter
```
Or add `ComputerysProfanityFilter` through the NuGet package manager in your IDE.

### Unity Package Manager
In Unity, open `Window > Package Manager`, select `+`, then choose `Add package from git URL...`, then enter:
```
https://github.com/C0mputery/ComputerysProfanityFilter.git?path=/ComputerysProfanityFilter#upm-1.1.0
```
The `#upm-1.1.0` suffix pins the package to version 1.1.0; change it to a newer `upm-x.y.z` tag to update.
The UPM package requires Unity 2021.3 or newer.

## Basic usage
Create one `ProfanityList` and reuse it when filtering messages.
After construction, the instance can be used concurrently from multiple threads.
`Censor` returns the original string unchanged when it finds no match.
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
ignores selected punctuation within words, and collapses consecutive repeated letters.
Entire matches, including their intervening punctuation, are replaced with the censor character.

Matching flexable, case-insensitive, it'll normalizes many substitutions (1337 speak, common enlgish rules).
Punctuation and whitespace may be inserted within terms without preventing a match.
Consecutive repeated letters are collapsed, so variants such as `foooool` can match `fool`.
Matchs are as terms rather than arbitrary substrings, avoiding the Scunthorpe problem, `cunt` does not match inside `Scunthorpe`.
Recognized punctuation can also act as a boundary between terms.
Entire matches, including intervening whitespace and punctuation, are replaced with the censor character.

## Detecting profanity
Use `HasProfanity` when you only need a yes/no result:
```csharp
bool containsProfanity = filter.HasProfanity("That is a$$hole behavior.");
```

Use `DetectAllProfanities` when you need the locations and configured terms that matched:
```csharp
var matches = filter.DetectAllProfanities("Sh!t happens.");

ProfanityMatch match = matches[0];
// match.Start: 0
// match.End: 4 (exclusive)
// match.Term: "shit"
```

`Start` is the zero-based inclusive index in the original input.
`End` is the zero-based exclusive index. A match's `Term` is the configured term, even when the input uses an expanded or obfuscated form.
Matches are returned in source order. Allow terms are excluded from all three operations.

## Custom term list
Pass your own terms and normalization rules to the constructor.
By default, supplied terms are expanded into several common English forms, so use base terms where appropriate.
Set `expandTermForms: false` to match only the supplied forms after normalization.

```csharp
using System.Collections.Generic;
using ComputerysProfanityFilter;

ProfanityList filter = new ProfanityList(
    terms: new[] { "spoiler", "example phrase" },
    allowTerms: new[] { "allowed word" },
    expandTermForms: false,
    expectedCharacters: DefaultProfanityList.ExpectedCharacters,
    joinerCharacters: DefaultProfanityList.JoinerCharacters,
    boundaryCharacters: new[] { '.', '-', '_' },
    characterMap: new Dictionary<char, string> { ['4'] = "a", ['@'] = "a" },
    sequenceMap: new Dictionary<string, string>()
);

string censored = filter.Censor("S-p-o-i-l-e-r ahead");
// censored: "############# ahead"
```

## Default configuration

`DefaultProfanityList` exposes the built-in settings as read-only collections for reuse in custom filters:
`Terms`, `AlwaysCensorTerms`, `AllowTerms`, `ExpectedCharacters`, `JoinerCharacters`, `BoundaryCharacters`, `CharacterMap`, and `SequenceMap`.
Allow terms are compared to the original matched text using lowercase-invariant normalization only.

### Allocation behavior
`Censor` is zero allocate on the heap when no term matches, it'll return the same `string` instance that was passed in.
Only when a match is found will it rent a buffer from `ArrayPool<char>.Shared` and returns a string that is the same length as the input.
The rented buffers can be reused across calls after the pool is warm, although rent may allocate when the pool has no suitable buffer available
(and there are some other caveats with `ArrayPool`, but)
Under typical use conditions, it will only allocate a string of the same size input.

## Comparisons
| Feature | ComputerysProfanityFilter | [Profanity.Detector](https://github.com/stephenhaunts/ProfanityDetector) 0.1.8 | [BogaNet.BadWordFilter](https://github.com/slaubenberger/BogaNet) | [DotnetBadWordDetector](https://github.com/FelipeLuz/dotnet-bad-word-detector-and-filter) | [Censored](https://github.com/jamesmontemagno/Censored) | [mk.profanity](https://github.com/mcknight89/mk.profanity) |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Distribution | [NuGet](https://www.nuget.org/packages/ComputerysProfanityFilter/) + UPM (see above) | [NuGet](https://www.nuget.org/packages/Profanity.Detector/) | [NuGet](https://www.nuget.org/packages/BogaNet.BadWordFilter/) | [NuGet](https://www.nuget.org/packages/DotnetBadWordDetector/) | [NuGet](https://www.nuget.org/packages/Censored/) | [NuGet](https://www.nuget.org/packages/mk.profanity/) |
| Matching approach | Deterministic normalized term matching | Dictionary/regex scanning + Scunthorpe heuristic | Regex corpus + optional normalization; optional simple matching | ML.NET binary classifiers | Escaped wildcard patterns compiled to regex | Tokenized exact list + optional Levenshtein |
| Case-insensitive matching | ✓ | ✓ | ✓ | ◐ It's ML | ✓ | ◐ |
| Word-boundary | ✓ | ◐ | ◐ | ◐ It's ML | ✓ | ◐ |
| Leetspeak normalization | ✓ | ✗ | ✓ | ◐ It's ML | ✗ | ✗ |
| Inserted punctuation handling | ✓ | ◐ | ◐ | ✓ | ◐ | ✗ |
| Inserted spaces handling | ✓ | ✗ | ◐ | ◐ It's ML | ◐ | ✗ |
| Repeated-letter handling | ✓ | ✗ | ✗ | ◐ It's ML | ✗ | ◐ |
| Multi-word phrase support | ✓ | ✓ | ✓ | ◐ | ✓ | ◐ |
| Match positions | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ |
| Custom list | ✓ | ✓ | ✓ | ✗ | ✓ | ✓ |
| Allow list | ✓ | ✓ | ✗ | ✗ | ✗ | ✓ |
| Configurable censor character | ✓ | ✓ | ✓ | ✓ | ✗ | ✓ |
| Arbitrary custom-language terms/rules | ✓ | ✓ | ✓ | ✗ It's ML | ✓ | ✓ |
| Concurrency/thread safety | ✓ Explicitly documented | Mutable, not documented | Mutable, not documented | ✗ Uses `PredictionEngine` | Mutable, not documented | Mutable, not documented |
| External dependencies | None | None | BogaNet.* infrastructure packages | Microsoft.ML 1.7.0 | None | None |
| Trimming/AOT compatibility | ✓ | ✓ | ◐ | ✗ | ✓ | ✓ |
| Bundled profanity data | ✓ 145 terms (2,573 with variations) | ✓ 1,617 terms, [the list](https://github.com/stephenhaunts/ProfanityDetector/blob/main/ProfanityFilter/ProfanityFilter/ProfanityList.cs) is weird, read it, very much quantity over quality) | ✓ ~4,600 entries (claims 5,000+ regexes) | ✓ 4 ML Models | ✗ | ✓ 2,915 words |
| Bundled languages | English | English | 25 | 4 | 0 | English |
| Fuzzy matching | ✗ | ✗ | ✗ | ◐ It's ML | ◐ | ✓ |
| Confidence | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Mutable | ✗ | ✓ | ✓ | ✗ | ✓ | ◐ |
| Patterns | ✗ | ✗ | ✓ Regex | ✗ | ✓ Wildcards | ✗ |
| URL/email moderation | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Other symbol/content filters | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Async rule loading | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Target framework | .NET Standard 2.1 and .NET 10 | .NET Standard 2.0 | .NET 8 | .NET 8 | .NET Standard 1.0 | .NET Standard 2.1 |
| License | Apache-2.0 | MIT | MIT | Apache-2.0 | MIT | MIT |

## Benchmarks

BenchmarkDotNet 0.15.8 results with .NET 10.0.10 on
an AMD Ryzen 7 5800X (Windows 11). The suite compares this library with
[`Profanity.Detector` 0.1.8](https://github.com/stephenhaunts/ProfanityDetector),
[BogaNet.BadWordFilter](https://github.com/slaubenberger/BogaNet),
[DotnetBadWordDetector](https://github.com/FelipeLuz/dotnet-bad-word-detector-and-filter),
[Censored](https://github.com/jamesmontemagno/Censored), and
[mk.profanity](https://github.com/mcknight89/mk.profanity),
using equivalent vocabularies where possible. Inputs are exactly 100, 1,000,
10,000, or 100,000 characters, made by repeating a fixed 10-message chat corpus
containing ordinary, profane, and obfuscated text.

Benchmark run date: August 21, 2026.

### Censoring

Results are grouped by vocabulary so each comparison uses the same term list.
Times are mean time per `Censor` operation; allocation is managed allocation per operation.

#### Expanded default vocabulary (2,573 terms)
| Mean Time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 2.468 us | 29.422 us | 302.077 us | 2.981 ms |
| Profanity.Detector | 109.157 us | 1.165 ms | 113.736 ms | 1.943 s |
| BogaNet.BadWordFilter | 151.999 us | 1.464 ms | 14.817 ms | 166.816 ms |
| DotnetBadWordDetector | 12.619 us | 118.059 us | 1.175 ms | 12.425 ms |
| Censored | 8.154 ms | 8.146 ms | 9.929 ms | 25.445 ms |
| mk.profanity | 51.646 us | 391.885 us | 3.838 ms | 39.767 ms |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | - | 2 KB | 20 KB | 195 KB |
| Profanity.Detector | 8 KB | 268 KB | 68.00 MB | 2.40 GB |
| BogaNet.BadWordFilter | 520 B | 29 KB | 2.04 MB | 205.42 MB |
| DotnetBadWordDetector | 3 KB | 24 KB | 222 KB | 2.15 MB |
| Censored | 14.09 MB | 14.17 MB | 14.29 MB | 15.52 MB |
| mk.profanity | 4 KB | 19 KB | 151 KB | 1.42 MB |

#### Profanity.Detector raw vocabulary (1,617 terms)
| Mean time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 2.903 us | 33.929 us | 336.551 us | 3.429 ms |
| Profanity.Detector | 72.138 us | 751.393 us | 73.684 ms | 1.379 s |
| BogaNet.BadWordFilter | 1.188 ms | 9.846 ms | 99.245 ms | 1.041 s |
| DotnetBadWordDetector | 13.296 us | 117.788 us | 1.173 ms | 12.609 ms |
| Censored | 2.872 ms | 3.163 ms | 4.114 ms | 12.828 ms |
| mk.profanity | 41.420 us | 340.211 us | 3.452 ms | 34.629 ms |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | - | 2 KB | 20 KB | 195 KB |
| Profanity.Detector | 5 KB | 290 KB | 49.33 MB | 2.11 GB |
| BogaNet.BadWordFilter | 520 B | 49 KB | 2.98 MB | 294.18 MB |
| DotnetBadWordDetector | 3 KB | 24 KB | 222 KB | 2.15 MB |
| Censored | 5.65 MB | 5.70 MB | 5.92 MB | 8.13 MB |
| mk.profanity | 4 KB | 20 KB | 152 KB | 1.42 MB |

#### CPU-stress input
This benchmark uses a repeated `аss…` input.
CPU-stress numbers re-run August 21, 2026.

| Mean Time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 6.768 us | 73.650 us | 761.768 us | 7.955 ms |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 224 B | 2 KB | 20 KB | 195 KB |

### Construction
Construction measurements create one filter instance. This system doesn't have free construction, so be a little bit wary of that. Make sure that you cache your instance after it's created.

| Engine and vocabulary | Mean | Allocated |
| --- | ---: | ---: |
| ComputerysProfanityFilter / expanded default (2,573 terms) | 4.616 ms | 5.39 MB |
| Profanity.Detector / same expanded vocabulary (2,573 terms) | 5.490 us | 32.98 KB |
| ComputerysProfanityFilter / Profanity.Detector raw vocabulary (1,617 terms) | 1.364 ms | 2.90 MB |
| Profanity.Detector / raw default vocabulary (1,617 terms) | 4.399 us | 25.58 KB |
| BogaNet.BadWordFilter / English sources | 2.225 ms | 1.64 MB |
| DotnetBadWordDetector / default model | 29.899 ms | 2.91 MB |
| Censored / equivalent expanded vocabulary | 770.0 ns | 20.18 KB |
| mk.profanity / equivalent expanded vocabulary | 761.4 ns | 20.30 KB |

## License
Copyright 2026 Christopher Rohland.
This project is licensed under the Apache License 2.0. See the [LICENSE](LICENSE) file for the full license text.
