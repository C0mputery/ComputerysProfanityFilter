using Xunit;

namespace ComputerysProfanityFilter.Tests;

public sealed class CensorExactMatchingTests {
    [Theory]
    [Trait("Feature", "Exact matching")]
    [InlineData("spoiler", "#######")]
    [InlineData("A SPOILER follows", "A ####### follows")]
    [InlineData("spoiler, then text", "#######, then text")]
    public void Censor_ReplacesWholeTermWithoutChangingSurroundingText(string input, string expected) {
        ProfanityList filter = CreateFilter("spoiler");
        Assert.Equal(expected, filter.Censor(input));
    }

    [Theory]
    [Trait("Feature", "Word boundaries")]
    [InlineData("xspoiler")]
    [InlineData("spoilerx")]
    [InlineData("xspoilerx")]
    public void Censor_DoesNotMatchTermInsideAnotherWord(string input) {
        ProfanityList filter = CreateFilter("spoiler");
        Assert.Equal(input, filter.Censor(input));
    }

    [Theory]
    [Trait("Feature", "Word boundaries")]
    [InlineData("cunt", "Scunthorpe")]
    [InlineData("ass", "assistant")]
    [InlineData("ass", "classic")]
    [InlineData("dick", "Dickens")]
    [InlineData("cock", "cocktail")]
    public void Censor_DoesNotMatchBlockedTermInsideBenignWord(string term, string input) {
        ProfanityList filter = CreateFilter(term);

        Assert.Equal(input, filter.Censor(input));
    }

    [Theory]
    [Trait("Feature", "Replacement character")]
    [InlineData("spoiler", '*', "*******")]
    [InlineData("spoiler", '~', "~~~~~~~")]
    [InlineData("spoiler", 'X', "XXXXXXX")]
    public void Censor_UsesRequestedReplacementCharacter(string input, char censorCharacter, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler").Censor(input, censorCharacter));
    }

    [Theory]
    [Trait("Feature", "Input validation")]
    [InlineData(null)]
    public void Censor_RejectsNullInput(string? input) { Assert.Throws<ArgumentNullException>(() => CreateFilter("spoiler").Censor(input!)); }

    [Theory]
    [Trait("Feature", "Input validation")]
    [InlineData("")]
    [InlineData("ordinary text")]
    [InlineData("123")]
    public void Censor_ReturnsInputUnchangedWhenNoTermCanMatch(string input) {
        Assert.Equal(input, CreateFilter("spoiler").Censor(input));
    }

    [Fact]
    [Trait("Feature", "Empty vocabulary")]
    public void Construction_RejectsAnEmptyTermList() {
        Assert.Throws<ArgumentException>(() => new ProfanityList([], expandTermForms: false));
    }

    private static ProfanityList CreateFilter(string term) => new ProfanityList([term], expandTermForms: false);
}

public sealed class CensorObfuscationTests {
    [Theory]
    [Trait("Feature", "Word boundaries")]
    [InlineData("\u2665shit", "\u2665####")]
    public void Censor_MatchesTermsAfterUnrecognizedSymbols(string input, string expected) {
        Assert.Equal(expected, CreateFilter("shit").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Leetspeak")]
    [InlineData("sp0!l3r", "#######")]
    [InlineData("5p0!l3r", "#######")]
    [InlineData("SP0!L3R", "#######")]
    [InlineData("sp6oiler", "########")]
    [InlineData("sp9oiler", "########")]
    public void Censor_NormalizesCharacterSubstitutions(string input, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Punctuation obfuscation")]
    [InlineData("sp.o_i-l/e\\r", "############")]
    [InlineData("sp\u200Boiler", "########")]
    [InlineData("s.p.o.i.l.e.r", "#############")]
    public void Censor_IgnoresConfiguredPunctuationAndInvisibleCharacters(string input, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Punctuation boundaries")]
    [InlineData("hello!shit", "hello!####")]
    [InlineData("x-shit", "x-####")]
    public void Censor_TreatsPunctuationAsABoundaryWhenItCannotContinueATerm(string input, string expected) {
        Assert.Equal(expected, new ProfanityList(["shit"], expandTermForms: false).Censor(input));
    }

    [Fact]
    [Trait("Feature", "Punctuation boundaries")]
    public void Censor_TreatsPunctuationAsABoundaryBeforeANewTerm() {
        ProfanityList filter = new ProfanityList(["shit"], expandTermForms: false);

        Assert.Equal("word.####", filter.Censor("word.shit"));
    }

    [Fact]
    [Trait("Feature", "Punctuation boundaries")]
    public void Censor_KeepsPunctuationInsideAContinuingTerm() {
        ProfanityList filter = new ProfanityList(["shit", "shithead", "fuck"], expandTermForms: false);

        Assert.Equal("#####", CreateFilter("shit").Censor("sh.it"));
        Assert.Equal("#########", filter.Censor("shit!head"));
        Assert.Equal("####!####", filter.Censor("shit!fuck"));
    }

    [Theory]
    [Trait("Feature", "Punctuation configuration")]
    [InlineData("sh.it", "#####")]
    [InlineData("x-shit", "x-####")]
    public void Censor_UsesCallerConfiguredJoinersAndBoundaries(string input, string expected) {
        ProfanityList filter = new ProfanityList(
            terms: ["shit"],
            allowTerms: DefaultProfanityList.AllowTerms,
            expandTermForms: false,
            expectedCharacters: DefaultProfanityList.ExpectedCharacters,
            joinerCharacters: ['.'],
            boundaryCharacters: ['-'],
            characterMap: DefaultProfanityList.CharacterMap,
            sequenceMap: DefaultProfanityList.SequenceMap
        );

        Assert.Equal(expected, filter.Censor(input));
    }

    [Theory]
    [Trait("Feature", "Repeated letters")]
    [InlineData("fol", "###")]
    [InlineData("fool", "####")]
    [InlineData("foooool", "#######")]
    [InlineData("fooooool", "########")]
    [InlineData("FOOOOOL", "#######")]
    public void Censor_CollapsesAllRepeatedLetters(string input, string expected) {
        Assert.Equal(expected, CreateFilter("fool").Censor(input));
    }

    [Fact]
    [Trait("Feature", "Allow terms")]
    public void Censor_AllowsOnlyExactDefaultAllowedTermsAfterRepeatedLetterCollapsing() {
        ProfanityList filter = new ProfanityList(["ass", "coon", "asshole"], expandTermForms: false);

        Assert.Equal("as con ### #### #######", filter.Censor("as con ass coon asshole"));
        Assert.Equal("AS ###", filter.Censor("AS a-s"));
    }

    [Theory]
    [Trait("Feature", "Repeated letters")]
    [InlineData("fooll", "#####")]
    [InlineData("foollll", "#######")]
    public void Censor_ExtendsMatchesForRepeatedFinalLetters(string input, string expected) {
        Assert.Equal(expected, CreateFilter("fool").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Repeated letters")]
    [InlineData("assfuckk", "########")]
    public void Censor_ExtendsOverlappingMatchesWithoutChangingTheResult(string input, string expected) {
        ProfanityList filter = new ProfanityList(["fuck", "assfuck"], expandTermForms: false);

        Assert.Equal(expected, filter.Censor(input));
    }

    [Theory]
    [Trait("Feature", "ASCII-art sequences")]
    [InlineData(@"|\/|@m@", "#######")]
    [InlineData(@"/\/\@m@", "#######")]
    [InlineData(@"m@|\/|@", "#######")]
    public void Censor_NormalizesConfiguredMultiCharacterSequences(string input, string expected) {
        Assert.Equal(expected, CreateFilter("mama").Censor(input));
    }

    private static ProfanityList CreateFilter(string term) => new ProfanityList([term], expandTermForms: false);
}

public sealed class CensorUnicodeNormalizationTests {
    [Theory]
    [Trait("Feature", "Latin diacritic mappings")]
    [InlineData("fuck", "fúck", "####")]
    [InlineData("fuck", "fúck", "#####")]
    [InlineData("shit", "shït", "####")]
    [InlineData("fuck", "ＦＵＣＫ", "####")]
    public void Censor_MatchesLatinAccentedAndCompatibilityForms(string term, string input, string expected) {
        Assert.Equal(expected, new ProfanityList([term], expandTermForms: false).Censor(input));
    }

    [Theory]
    [Trait("Feature", "Unicode compatibility normalization")]
    [InlineData("ｓｐｏｉｌｅｒ", "#######")]
    [InlineData("ⓢⓟⓞⓘⓛⓔⓡ", "#######")]
    [InlineData("ＳＰＯＩＬＥＲ", "#######")]
    public void Censor_MatchesCompatibilityFormsOfLetters(string input, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Unicode compatibility normalization")]
    [InlineData("stupid", "ﬆupid", "#####")]
    [InlineData("office", "oﬃce", "####")]
    [InlineData("afflict", "aﬄict", "#####")]
    public void Censor_MatchesCompatibilityLigaturesThatExpandToMultipleLetters(string term, string input, string expected) {
        Assert.Equal(expected, CreateFilter(term).Censor(input));
    }

    private static ProfanityList CreateFilter(string term) => new ProfanityList([term], expandTermForms: false);
}

public sealed class CensorPhraseAndConfigurationTests {
    [Theory]
    [Trait("Feature", "Phrase matching")]
    [InlineData("spoiler alert", "#############")]
    [InlineData("spoiler\talert", "#############")]
    [InlineData("spoiler     alert", "#################")]
    public void Censor_ReplacesFullPhraseIncludingItsWhitespace(string input, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler alert").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Whitespace obfuscation")]
    [InlineData("sp oil er", "#########")]
    [InlineData("s poi ler", "#########")]
    [InlineData("spo il er", "#########")]
    public void Censor_DetectsTermSplitAcrossWhitespace(string input, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Term-form expansion")]
    [InlineData("spoilers", "########")]
    [InlineData("spoilering", "##########")]
    [InlineData("spoilerish", "##########")]
    public void Censor_MatchesGeneratedEnglishFormsByDefault(string input, string expected) {
        Assert.Equal(expected, new ProfanityList(["spoiler"]).Censor(input));
    }

    [Theory]
    [Trait("Feature", "Term-form expansion")]
    [InlineData("spoilers", "spoilers")]
    [InlineData("spoilering", "spoilering")]
    [InlineData("spoilerish", "spoilerish")]
    public void Censor_DoesNotMatchGeneratedFormsWhenExpansionIsDisabled(string input, string expected) {
        Assert.Equal(expected, CreateFilter("spoiler").Censor(input));
    }

    [Theory]
    [Trait("Feature", "Repeated letters")]
    [InlineData("a a", "# #")]
    public void Censor_ResetsRepeatedLetterTrackingAtWhitespace(string input, string expected) {
        Assert.Equal(expected, CreateFilter("a").Censor(input));
    }

    private static ProfanityList CreateFilter(string term) => new ProfanityList([term], expandTermForms: false);
}
