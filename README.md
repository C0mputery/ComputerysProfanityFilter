# ComputerysProfanityFilter

A .NET Standard 2.1 (C# version 9 for unity) profanity filter that censors known terms while recognizing common
obfuscations such as character substitutions, punctuation, inserted whitespace, repeated letters, and leetspeak.
The [default term list](ComputerysProfanityFilter/DefaultProfanityList.cs) is reasonable and covers swears, hate speech, and self-harm.

This library is currently used in [STRAFTAT](https://store.steampowered.com/app/2386720/STRAFTAT/) (as of the 1.4.9 update) to filter text chat, player names, lobby names, etc, albeit with a slightly cut down word list (swears removed).

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
`Terms`, `AllowTerms`, `ExpectedCharacters`, `JoinerCharacters`, `BoundaryCharacters`, `CharacterMap`, and `SequenceMap`.
Allow terms are compared to the original matched text using lowercase-invariant normalization only.

### Allocation behavior
`Censor` is zero allocate on the heap when no term matches, it'll return the same `string` instance that was passed in.
Only when a match is found will it rent a buffer from `ArrayPool<char>.Shared` and returns a string that is the same length as the input.
The rented buffers can be reused across calls after the pool is warm, although rent may allocate when the pool has no suitable buffer available
(and there are some other caveats with `ArrayPool`, but)
Under typical use conditions, it will only allocate a string of the same size input.

## Benchmarks

BenchmarkDotNet 0.15.8 results with .NET 10.0.10 on
an AMD Ryzen 7 5800X (Windows 11). The suite compares this library with
[`Profanity.Detector 0.1.8`](https://github.com/stephenhaunts/ProfanityDetector)
using equivalent vocabularies. Inputs are exactly 100, 1,000, 10,000, or
100,000 characters, made by repeating a fixed 10-message chat corpus containing
ordinary, profane, and obfuscated text.

Benchmark run date: August 19, 2026.

### Censoring

Results are grouped by vocabulary so each comparison uses the same term list.
Times are mean time per `Censor` operation; allocation is managed allocation per operation.

#### Expanded default vocabulary (2,578 terms)
| Mean Time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 2.180 us | 25.718 us | 254.357 us | 2.657 ms |
| Profanity.Detector | 141.017 us | 916.118 us | 110.825 ms | 2.035 s |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | - | 2 KB | 20 KB | 195 KB |
| Profanity.Detector | 8 KB | 268 KB | 64.90 MB | 2.23 GB |

#### Profanity.Detector raw vocabulary (1,626 terms)
| Mean time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 2.676 us | 30.427 us | 302.885 us | 3.151 ms |
| Profanity.Detector | 75.500 us | 638.787 us | 74.206 ms | 1.500 s |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | - | 2 KB | 20 KB | 195 KB |
| Profanity.Detector | 5 KB | 290 KB | 46.96 MB | 1.96 GB |

#### CPU-stress input
This benchmark uses a repeated `⚡⚡` input as it was the slowest thing I could find.

| Mean Time | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | 4.449 us | 41.877 us | 415.244 us | 4.143 ms |

| Managed Allocation | 100 chars | 1,000 chars | 10,000 chars | 100,000 chars |
| --- | ---: | ---: | ---: | ---: |
| ComputerysProfanityFilter | - | - | - | - |

### Construction
Construction measurements create one filter instance. This system doesn't have free construction, so be a little bit wary of that. Make sure that you cache your instance after it's created.

| Engine and vocabulary | Mean | Allocated |
| --- | ---: | ---: |
| ComputerysProfanityFilter / expanded default (2,578 terms) | 4.656 ms | 5.13 MB |
| Profanity.Detector / same expanded vocabulary (2,578 terms) | 6.251 us | 33.05 KB |
| ComputerysProfanityFilter / Profanity.Detector raw vocabulary (1,626 terms) | 1.440 ms | 2.70 MB |
| Profanity.Detector / raw default vocabulary (1,626 terms) | 5.200 us | 25.58 KB |

## License
Copyright 2026 Christopher Rohland.
This project is licensed under the Apache License 2.0. See the [LICENSE](LICENSE) file for the full license text.
