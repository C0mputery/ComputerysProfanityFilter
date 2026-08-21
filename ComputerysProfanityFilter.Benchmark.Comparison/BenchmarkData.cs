using System.Reflection;
using ComputeryFilter = ComputerysProfanityFilter.ProfanityList;
using StephenFilter = ProfanityFilter.ProfanityFilter;
using BogaFilter = BogaNet.BWF.Filter.BadWordFilter;
using BogaConstants = BogaNet.BWF.BWFConstants;
using DotnetDetector = DotnetBadWordDetector.ProfanityDetector;
using CensoredCensor = Censored.Censor;
using MkFilter = mk.profanity.ProfanityFilter;

public static class BenchmarkData {
    private const string CpuStressText = "\u0430ss\u2026";

    private static readonly string[] TypicalCorpus = [
        // Ordinary text and exact whole-word matches.
        "Please keep the conversation friendly and helpful.",
        "This sentence has a profanity in the middle of otherwise ordinary text: shit.",
        "Word boundaries matter: Scunthorpe, xspoiler, spoilerx, and xspoilerx remain ordinary text.",

        // Skippable punctuation, invisible characters, and whitespace obfuscation.
        "Punctuation surrounds obfuscated words: a.s.s_h-o/l\\e, sh!t, and f-u-c-k.",
        "Invisible separators include sp\u200Boiler, while whitespace splits sp oil er and s poi ler.",
        "Punctuation, commas, periods, and symbols !@#$%^ remain realistic workload noise.",

        // Character and multi-character sequence maps (leetspeak and ASCII art).
        "Character substitutions include 5h1t, a$$h0le, and f4gg0t.",
        @"ASCII-art substitutions include |\/|@m@, /\/\@m@, and |3an3r.",

        // Repeated-letter collapsing, final-letter extension, and overlapping matches.
        "Repeated letters include shiiit, fuuuuuck, and assfuckkk without changing the scan shape.",
        "Overlapping terms such as assfuck and its repeated final letter assfuckk exercise pending matches.",
        "Separate short matches reset state at whitespace: a a should be scanned independently.",

        // Phrases, generated English forms, and configured hate/self-harm phrases.
        "Multi word phrases include son of a bitch, kill yourself, and gas the jews.",
        "Generated forms include fuckers, fucking, fuckish, and assholes.",

        // Compatibility characters and ligatures are normalized before matching.
        "Compatibility forms include ï½“ï½ï½ï½‰ï½Œï½…ï½’ and ligatures such as oï¬ƒce.",

        // A longer realistic message prevents scanning from finishing immediately.
        "A longer chat line contains several ordinary words before a potentially offensive phrase appears near the end, so scanning cannot finish immediately.",
        "Short messages are common in games, but long messages are important too because allocation and scanning costs scale with text length."
    ];

    private static readonly string TypicalText = string.Join(" ", TypicalCorpus) + " ";
    private static readonly string[] OwnExpandedWords = GetOwnExpandedWords();
    private static readonly string[] StephenWords = GetStephenWords();

    public static string CreateTypicalMessage(int length) => CreateMessage(TypicalText, length);

    public static string CreateCpuStressMessage(int length) => CreateMessage(CpuStressText, length);

    public static ComputeryFilter CreateComputerysDefaultFilter() => new ComputeryFilter();

    public static StephenFilter CreateStephenExpandedFilter() => new StephenFilter(OwnExpandedWords);

    public static ComputeryFilter CreateComputerysStephenRawFilter() => new ComputeryFilter(
        StephenWords,
        ComputerysProfanityFilter.DefaultProfanityList.AlwaysCensorTerms,
        ComputerysProfanityFilter.DefaultProfanityList.AllowTerms,
        false,
        ComputerysProfanityFilter.DefaultProfanityList.ExpectedCharacters,
        ComputerysProfanityFilter.DefaultProfanityList.JoinerCharacters,
        ComputerysProfanityFilter.DefaultProfanityList.BoundaryCharacters,
        ComputerysProfanityFilter.DefaultProfanityList.CharacterMap,
        ComputerysProfanityFilter.DefaultProfanityList.SequenceMap
    );

    public static StephenFilter CreateStephenDefaultFilter() => new StephenFilter();

    public static BogaFilter CreateBogaCustomFilter(string[] words) {
        BogaFilter.Instance.Clear();
        BogaFilter.Instance.Add(true, "benchmark", words);
        return BogaFilter.Instance;
    }

    public static DotnetDetector CreateDotnetDetector() => new DotnetDetector();

    public static CensoredCensor CreateCensoredFilter(string[] words) => new CensoredCensor(words);

    public static MkFilter CreateMkFilter(string[] words) => new MkFilter(options => options.SetBadWords(words));

    public static bool LoadBogaEnglishSources() {
        BogaFilter.Instance.Clear();
        return BogaFilter.Instance.LoadFiles(true, BogaConstants.BWF_EN);
    }

    private static string CreateMessage(string text, int length) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        int repetitions = (length + text.Length - 1) / text.Length;
        return string.Concat(Enumerable.Repeat(text, repetitions))[..length];
    }

    private static string[] GetOwnExpandedWords() {
        ComputeryFilter filter = CreateComputerysDefaultFilter();
        return [.. filter.GenerateEncodedVariations(ComputerysProfanityFilter.DefaultProfanityList.Terms).Keys.OrderBy(word => word, StringComparer.Ordinal)];
    }

    public static string[] GetExpandedWords() => OwnExpandedWords;

    public static string[] GetStephenRawWords() => StephenWords;

    private static string[] GetStephenWords() {
        StephenFilter filter = CreateStephenDefaultFilter();
        FieldInfo field = filter.GetType().BaseType!.GetField("_wordList", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return [.. (string[])field.GetValue(filter)!];
    }
}
