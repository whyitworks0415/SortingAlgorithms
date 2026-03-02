using SortingVisualizerApp.Core;

var registry = new AlgorithmRegistry();
var suites = new List<ValidationSuiteResult>
{
    RegistryValidationSuite.Run(registry),
    ComplexityMapValidationSuite.Run(registry),
    BarsValidationSuite.Run(registry),
    HeapTreeValidationSuite.Run(registry),
    PhaseCCompletionValidationSuite.Run(registry),
    PhaseDCompletionValidationSuite.Run(registry),
    MissingAdvancedAlgorithmsValidationSuite.Run(registry),
    SpecialAlgorithmValidationSuite.Run(registry),
    ParallelValidationSuite.Run(registry),
    GpuMassiveValidationSuite.Run(registry),
    StableValidationSuite.Run(registry),
    NetworkValidationSuite.Run(registry),
    ExternalValidationSuite.Run(registry),
    GraphValidationSuite.Run(registry),
    StringValidationSuite.Run(registry),
    SpatialValidationSuite.Run(registry)
};

var totalFailures = suites.Sum(static suite => suite.Failures.Count);
var totalRuns = suites.Sum(static suite => suite.Runs);

foreach (var suite in suites)
{
    Console.WriteLine($"[{suite.Name}] runs={suite.Runs} failures={suite.Failures.Count}");
    foreach (var note in suite.Notes)
    {
        Console.WriteLine($"  note: {note}");
    }

    foreach (var failure in suite.Failures.Take(25))
    {
        Console.WriteLine($"  fail: {failure}");
    }

    if (suite.Failures.Count > 25)
    {
        Console.WriteLine($"  ... {suite.Failures.Count - 25} more failure(s)");
    }
}

if (totalFailures == 0)
{
    Console.WriteLine($"PASS: {suites.Count} suites, {totalRuns} runs.");
    return 0;
}

Console.WriteLine($"FAIL: {totalFailures} issue(s) across {suites.Count} suites.");
return 1;
