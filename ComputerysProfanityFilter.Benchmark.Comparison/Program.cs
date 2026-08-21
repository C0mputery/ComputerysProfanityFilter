using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ComputeryFilter = ComputerysProfanityFilter.ProfanityList;
using StephenFilter = ProfanityFilter.ProfanityFilter;
using BogaFilter = BogaNet.BWF.Filter.BadWordFilter;
using DotnetDetector = DotnetBadWordDetector.ProfanityDetector;
using CensoredCensor = Censored.Censor;
using MkFilter = mk.profanity.ProfanityFilter;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator));

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class ExpandedVocabularyCensorBenchmarks {
    [Params(100, 1_000, 10_000, 100_000)] public int MessageLength { get; set; }

    private ComputeryFilter _computerys = null!;
    private StephenFilter _stephen = null!;
    private BogaFilter _boga = null!;
    private DotnetDetector _dotnet = null!;
    private CensoredCensor _censored = null!;
    private MkFilter _mk = null!;
    private string _input = null!;

    [GlobalSetup]
    public void Setup() {
        _computerys = BenchmarkData.CreateComputerysDefaultFilter();
        _stephen = BenchmarkData.CreateStephenExpandedFilter();
        _boga = BenchmarkData.CreateBogaCustomFilter(BenchmarkData.GetExpandedWords());
        _dotnet = BenchmarkData.CreateDotnetDetector();
        _censored = BenchmarkData.CreateCensoredFilter(BenchmarkData.GetExpandedWords());
        _mk = BenchmarkData.CreateMkFilter(BenchmarkData.GetExpandedWords());
        _input = BenchmarkData.CreateTypicalMessage(MessageLength);
    }

    [Benchmark(Baseline = true, Description = "ComputerysProfanityFilter")]
    public int ComputerysProfanityFilter() => _computerys.Censor(_input).Length;

    [Benchmark(Description = "Profanity.Detector")]
    public int ProfanityDetector() => _stephen.CensorString(_input).Length;

    [Benchmark(Description = "BogaNet.BadWordFilter")]
    public int BogaNetBadWordFilter() => _boga.ReplaceAll(_input).Length;

    [Benchmark(Description = "DotnetBadWordDetector")]
    public int DotnetBadWordDetector() => _dotnet.MaskProfanity(_input).Length;

    [Benchmark(Description = "Censored")]
    public int Censored() => _censored.CensorText(_input).Length;

    [Benchmark(Description = "mk.profanity")]
    public int MkProfanity() => _mk.CensorText(_input).Length;
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class RawVocabularyCensorBenchmarks {
    [Params(100, 1_000, 10_000, 100_000)] public int MessageLength { get; set; }

    private ComputeryFilter _computerys = null!;
    private StephenFilter _stephen = null!;
    private BogaFilter _boga = null!;
    private DotnetDetector _dotnet = null!;
    private CensoredCensor _censored = null!;
    private MkFilter _mk = null!;
    private string _input = null!;

    [GlobalSetup]
    public void Setup() {
        _computerys = BenchmarkData.CreateComputerysStephenRawFilter();
        _stephen = BenchmarkData.CreateStephenDefaultFilter();
        _boga = BenchmarkData.CreateBogaCustomFilter(BenchmarkData.GetStephenRawWords());
        _dotnet = BenchmarkData.CreateDotnetDetector();
        _censored = BenchmarkData.CreateCensoredFilter(BenchmarkData.GetStephenRawWords());
        _mk = BenchmarkData.CreateMkFilter(BenchmarkData.GetStephenRawWords());
        _input = BenchmarkData.CreateTypicalMessage(MessageLength);
    }

    [Benchmark(Baseline = true, Description = "ComputerysProfanityFilter")]
    public int ComputerysProfanityFilter() => _computerys.Censor(_input).Length;

    [Benchmark(Description = "Profanity.Detector")]
    public int ProfanityDetector() => _stephen.CensorString(_input).Length;

    [Benchmark(Description = "BogaNet.BadWordFilter")]
    public int BogaNetBadWordFilter() => _boga.ReplaceAll(_input).Length;

    [Benchmark(Description = "DotnetBadWordDetector")]
    public int DotnetBadWordDetector() => _dotnet.MaskProfanity(_input).Length;

    [Benchmark(Description = "Censored")]
    public int Censored() => _censored.CensorText(_input).Length;

    [Benchmark(Description = "mk.profanity")]
    public int MkProfanity() => _mk.CensorText(_input).Length;
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class ConstructionBenchmarks {
    [Benchmark(Baseline = true, Description = "ComputerysProfanityFilter / expanded default")]
    public ComputeryFilter ConstructComputerysDefault() => BenchmarkData.CreateComputerysDefaultFilter();

    [Benchmark(Description = "Profanity.Detector / equivalent expanded vocabulary")]
    public StephenFilter ConstructStephenExpanded() => BenchmarkData.CreateStephenExpandedFilter();

    [Benchmark(Description = "ComputerysProfanityFilter / Profanity.Detector raw vocabulary")]
    public ComputeryFilter ConstructComputerysStephenRaw() => BenchmarkData.CreateComputerysStephenRawFilter();

    [Benchmark(Description = "Profanity.Detector / raw default vocabulary")]
    public StephenFilter ConstructStephenDefault() => BenchmarkData.CreateStephenDefaultFilter();

    [Benchmark(Description = "BogaNet.BadWordFilter / English sources")]
    public bool ConstructBogaEnglish() => BenchmarkData.LoadBogaEnglishSources();

    [Benchmark(Description = "DotnetBadWordDetector / default model")]
    public DotnetDetector ConstructDotnetDetector() => BenchmarkData.CreateDotnetDetector();

    [Benchmark(Description = "Censored / equivalent expanded vocabulary")]
    public CensoredCensor ConstructCensoredExpanded() => BenchmarkData.CreateCensoredFilter(BenchmarkData.GetExpandedWords());

    [Benchmark(Description = "mk.profanity / equivalent expanded vocabulary")]
    public MkFilter ConstructMkExpanded() => BenchmarkData.CreateMkFilter(BenchmarkData.GetExpandedWords());
}
