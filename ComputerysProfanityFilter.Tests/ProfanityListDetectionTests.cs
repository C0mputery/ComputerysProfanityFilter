using Xunit;

namespace ComputerysProfanityFilter.Tests;

public sealed class ProfanityListDetectionTests {
    [Fact]
    public void HasProfanity_FindsAndRejectsDirectTerms() {
        ProfanityList filter = CreateFilter(["shit"]);

        Assert.True(filter.HasProfanity("SHIT"));
        Assert.True(filter.HasProfanity("some shit here"));
        Assert.False(filter.HasProfanity("ship"));
        Assert.False(filter.HasProfanity("shitake"));
    }

    [Fact]
    public void DefaultFilter_DetectsEveryDefaultTerm() {
        ProfanityList filter = new ProfanityList();

        foreach (string term in DefaultProfanityList.Terms) {
            Assert.True(filter.HasProfanity(term), $"Default filter did not detect '{term}'.");
        }
    }

    [Fact]
    public void DefaultFilter_AlwaysCensorsHateSymbolsInsideWords() {
        ProfanityList filter = new ProfanityList();

        foreach (string term in DefaultProfanityList.AlwaysCensorTerms) {
            string input = $"prefix{term}suffix";
            Assert.True(filter.HasProfanity(input), $"Default filter did not detect '{term}'.");
            Assert.Equal($"prefix{new string('#', term.Length)}suffix", filter.Censor(input));

            ProfanityMatch match = Assert.Single(filter.DetectAllProfanities(input));
            Assert.Equal((6, 6 + term.Length, term), (match.Start, match.End, match.Term));
        }
    }

    [Fact]
    public void AlwaysCensorTerms_MatchInsideWordsIndependentOfNormalTerms() {
        ProfanityList filter = new ProfanityList(
            terms: ["symbolic"],
            allowTerms: [],
            alwaysCensorTerms: ["symbol"],
            expandTermForms: false
        );

        Assert.Equal("prefix######icsuffix", filter.Censor("prefixsymbolicsuffix"));
        ProfanityMatch match = Assert.Single(filter.DetectAllProfanities("prefixsymbolicsuffix"));
        Assert.Equal((6, 12, "symbol"), (match.Start, match.End, match.Term));
    }

    [Fact]
    public void CustomFilter_DoesNotSilentlyUseDefaultAlwaysCensorTerms() {
        ProfanityList filter = new ProfanityList(["spoiler"], expandTermForms: false);
        string symbol = DefaultProfanityList.AlwaysCensorTerms[0];

        Assert.False(filter.HasProfanity($"prefix{symbol}suffix"));
    }

    [Fact]
    public void AlwaysCensorSymbols_DoNotSuppressAdjacentNormalMatches() {
        ProfanityList filter = new ProfanityList(
            terms: ["bad"],
            allowTerms: [],
            alwaysCensorTerms: ["☃"],
            expandTermForms: false
        );

        Assert.Equal("####", filter.Censor("bad☃"));
        Assert.Collection(
            filter.DetectAllProfanities("bad☃"),
            match => Assert.Equal((0, 3, "bad"), (match.Start, match.End, match.Term)),
            match => Assert.Equal((3, 4, "☃"), (match.Start, match.End, match.Term))
        );
    }

    [Fact]
    public void AlwaysCensorTerms_AreIgnoredWhenMatchingNormalTerms() {
        ProfanityList filter = new ProfanityList();

        Assert.Equal("#####", filter.Censor("fꖦuck"));
    }

    [Fact]
    public void DetectAllProfanities_ReturnsObfuscatedAndRepeatedTermsInSourceOrder() {
        ProfanityList filter = CreateFilter(["shit", "fool"], new Dictionary<char, string> { ['!'] = "i" });

        IReadOnlyList<ProfanityMatch> matches = filter.DetectAllProfanities("Sh!t then foooool and shit");

        Assert.Collection(matches,
            match => { Assert.Equal((0, 4, "shit"), (match.Start, match.End, match.Term)); },
            match => { Assert.Equal((10, 17, "fool"), (match.Start, match.End, match.Term)); },
            match => { Assert.Equal((22, 26, "shit"), (match.Start, match.End, match.Term)); });

        Assert.Equal("#### then ####### and ####", filter.Censor("Sh!t then foooool and shit"));
    }

    [Fact]
    public void DetectAllProfanities_ReturnsCanonicalTermForGeneratedFormsAndPhrases() {
        ProfanityList filter = CreateFilter(["spoiler", "bad word"], expandTermForms: true);

        IReadOnlyList<ProfanityMatch> matches = filter.DetectAllProfanities("spoilers and bad   word");

        Assert.Collection(matches,
            match => { Assert.Equal((0, 8, "spoiler"), (match.Start, match.End, match.Term)); },
            match => { Assert.Equal((13, 23, "bad word"), (match.Start, match.End, match.Term)); });
    }

    [Fact]
    public void DetectAllProfanities_SuppressesAllowTermsAndConsolidatesOverlaps() {
        ProfanityList filter = CreateFilter(["ass", "assfuck", "fuck"], allowTerms: ["ass"]);

        IReadOnlyList<ProfanityMatch> matches = filter.DetectAllProfanities("ass assfuckk");

        ProfanityMatch match = Assert.Single(matches);
        Assert.Equal((4, 12, "assfuck"), (match.Start, match.End, match.Term));
    }

    [Fact]
    public void Detection_HandlesEmptyAndNullInput() {
        ProfanityList filter = CreateFilter(["shit"]);

        Assert.Empty(filter.DetectAllProfanities(string.Empty));
        Assert.False(filter.HasProfanity(string.Empty));
        Assert.Throws<ArgumentNullException>(() => filter.DetectAllProfanities(null!));
        Assert.Throws<ArgumentNullException>(() => filter.HasProfanity(null!));
    }

    private static ProfanityList CreateFilter(
        IEnumerable<string> terms,
        IEnumerable<KeyValuePair<char, string>>? characterMap = null,
        IEnumerable<string>? allowTerms = null,
        bool expandTermForms = false) => new ProfanityList(
        terms,
        Array.Empty<string>(),
        allowTerms ?? [],
        expandTermForms,
        "abcdefghijklmnopqrstuvwxyz",
        ".",
        "-_!",
        characterMap ?? [],
        []);
}
