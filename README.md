# ComputerysProfanityFilter

A .NET Standard 2.1 (C# version 9 for unity) profanity filter that censors known words while recognizing common
obfuscations such as character substitutions, punctuation, repeated letters, and leetspeak.

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
Pass your own normalization rules and words to the six-argument constructor. The supplied words
are also expanded into several common English forms, so use base words where appropriate.

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
