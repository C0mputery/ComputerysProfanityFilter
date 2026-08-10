Text encoding is intentionally duplicated because I can't figure out a way to de-dupe without major performance impacts.

- `ProfanityList.Encode` materializes encoded text and its source spans.
- `ProfanityList.Censor` performs the same encoding incrementally while advancing the matcher.

The pipeline as it stands now is:

1. Trim trailing characters from `_ignorableCharacters`.
   This happens before sequence and character mappings, including for characters that also have mappings.
   Consequently, trailing mapped punctuation cannot participate in encoding.
   For example, `fuc|<` encodes as `fuc`, not `fuck`, and `as$` encodes as `as`, not `ass`. Known issue, but trying to solve it is very hard! I failed.

2. Collapse consecutive whitespace and `Boundary` characters into one `Boundary`.
   This happens before sequence and character mappings, so whitespace and `Boundary` cannot participate in mappings.

3. At each remaining position, use the longest case-insensitive sequence mapping.
   Sequence mappings take priority over all single-character rules.

4. Otherwise, invariant-lowercase one character and apply its character mapping. If it has no mapping, discard it when ignorable or emit it unchanged.

5. Invariant-lowercase mapped output and collapse repetitions to two characters in `_allowsDouble`, or one for all other characters.
   Ignored input does not interrupt a repetition run.

`Encode` prepends a `Boundary` and, when content does not already end in one, appends a `Boundary`.
These synthetic boundaries have zero-length source spans, `[0, 0)` and `[end, end)`.

Every retained encoded character has a half-open source span, `[Start, End)`. Mapping expansions share their source span.
A collapsed repetition extends the previous span instead of producing another encoded character.

`Censor` advances the matcher only for retained encoded characters.
On a match, it censors from the first matched span's start through the last matched span's end.
It must also censor repetitions that collapse after a completed match.

Profanity patterns pass through `Encode` before entering the matcher, so materialized pattern encoding and streaming input encoding must remain equivalent.
