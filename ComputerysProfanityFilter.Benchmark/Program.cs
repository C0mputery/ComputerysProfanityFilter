using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ComputeryFilter = ComputerysProfanityFilter.ProfanityList;
using StephenFilter = ProfanityFilter.ProfanityFilter;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator));

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class CensorBenchmarks {
    [Params(BenchmarkData.ComputerysOwnExpanded, BenchmarkData.StephenOwnExpanded, BenchmarkData.ComputerysStephenRaw, BenchmarkData.StephenStephenRaw)]
    public string Configuration { get; set; } = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int MessageLength { get; set; }

    private Func<string, string> _censor = null!;
    private string _input = null!;

    [GlobalSetup]
    public void Setup() {
        _censor = BenchmarkData.CreateCensor(Configuration);
        _input = BenchmarkData.CreateMessage(MessageLength);
    }

    [Benchmark]
    public int CensorMessage() => _censor(_input).Length;
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class ConstructionBenchmarks {
    [Params(BenchmarkData.ComputerysOwnExpanded, BenchmarkData.StephenOwnExpanded,
        BenchmarkData.ComputerysStephenRaw, BenchmarkData.StephenStephenRaw)]
    public string Configuration { get; set; } = null!;

    [Benchmark]
    public object ConstructFilter() => BenchmarkData.CreateFilter(Configuration);
}

public static class BenchmarkData {
    public const string ComputerysOwnExpanded = "Computerys / own expanded (5352)";
    public const string StephenOwnExpanded = "StephenHaunts / own expanded (5352)";
    public const string ComputerysStephenRaw = "Computerys / StephenHaunts raw source (1626)";
    public const string StephenStephenRaw = "StephenHaunts / StephenHaunts raw source (1626)";
    public static readonly string[] Corpus = {
        "Please keep the conversation friendly and helpful.",
        "This sentence has a profanity in the middle of otherwise ordinary text.",
        "Repeated words should not change the result when a message is scanned again.",
        "Punctuation, commas, and periods surround words in real player chat.",
        "Multi word expressions are included in this moderately sized benchmark message.",
        "Obfuscated input such as a$$hole, sh1t, and f-u-c-k should exercise normalization.",
        "The town of Scunthorpe is included to exercise partial-match handling.",
        "A longer chat line contains several ordinary words before a potentially offensive phrase appears near the end, so scanning cannot finish immediately.",
        "Numbers 123456 and symbols !@#$%^ should remain realistic workload noise.",
        "Short messages are common in games, but long messages are important too because allocation and scanning costs scale with text length."
    };

    private static readonly string CorpusText = string.Join(" ", Corpus) + " ";

    private static readonly string[] OwnWords = [.. ComputerysProfanityFilter.DefaultProfanityList.Words];
    private static readonly string[] OwnExpandedWords = GetOwnExpandedWords(OwnWords);
    private static readonly string[] StephenWords = GetStephenWords();

    public static string CreateMessage(int length) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        int repetitions = (length + CorpusText.Length - 1) / CorpusText.Length;
        return string.Concat(Enumerable.Repeat(CorpusText, repetitions))[..length];
    }

    public static Func<string, string> CreateCensor(string configuration) {
        object filter = CreateFilter(configuration);
        return filter switch {
            ComputeryFilter computerys => input => computerys.Censor(input),
            StephenFilter stephen => stephen.CensorString,
            _ => throw new InvalidOperationException("Unsupported filter type.")
        };
    }

    public static object CreateFilter(string configuration) => configuration switch {
        ComputerysOwnExpanded => new ComputeryFilter(),
        StephenOwnExpanded => new StephenFilter(OwnExpandedWords),
        ComputerysStephenRaw => new ComputeryFilter(
            boundary: '\uffff',
            characterMap: ComputerysProfanityFilter.DefaultProfanityList.CharacterMap,
            sequenceMap: ComputerysProfanityFilter.DefaultProfanityList.SequenceMap,
            ignorableCharacters: ComputerysProfanityFilter.DefaultProfanityList.IgnorableCharacters,
            allowsEnglishDouble: ComputerysProfanityFilter.DefaultProfanityList.AllowsDouble,
            words: StephenWords,
            expandWordForms: false),
        StephenStephenRaw => new StephenFilter(),
        _ => throw new ArgumentOutOfRangeException(nameof(configuration), configuration, "Unknown benchmark configuration.")
    };

    private static string[] GetOwnExpandedWords(string[] words) {
        ComputeryFilter filter = new ComputeryFilter();
        MethodInfo method = typeof(ComputeryFilter).GetMethod("PopulateVariations", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((IEnumerable<string>)method.Invoke(filter, [words])!).OrderBy(word => word, StringComparer.Ordinal).ToArray();
    }

    private static string[] GetStephenWords() {
        StephenFilter filter = new StephenFilter();
        FieldInfo field = filter.GetType().BaseType!.GetField("_wordList", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return [.. (string[])field.GetValue(filter)!];
    }
}
