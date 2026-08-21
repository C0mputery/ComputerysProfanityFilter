using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator));

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 3, iterationCount: 7)]
public class CpuStressCensorBenchmarks {
    [Params(100, 1_000, 10_000, 100_000)] public int MessageLength { get; set; }

    private ComputerysProfanityFilter.ProfanityList _filter = null!;
    private string _input = null!;

    [GlobalSetup]
    public void Setup() {
        _filter = new ComputerysProfanityFilter.ProfanityList();
        _input = CreateMessage(MessageLength);
    }

    [Benchmark(Description = "ComputerysProfanityFilter")]
    public int ComputerysProfanityFilter() => _filter.Censor(_input).Length;

    private static string CreateMessage(int length) {
        const string text = "\u0430ss\u2026";
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        int repetitions = (length + text.Length - 1) / text.Length;
        return string.Concat(Enumerable.Repeat(text, repetitions))[..length];
    }
}
