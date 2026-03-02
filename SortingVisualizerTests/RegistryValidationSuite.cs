using SortingVisualizerApp.Core;

internal static class RegistryValidationSuite
{
    public static ValidationSuiteResult Run(AlgorithmRegistry registry)
    {
        var failures = RegistryMetadataValidator.Validate(registry, includeStableSmoke: true).ToList();
        var notes = new List<string>
        {
            "Checks: duplicate id, supported views, A/B/factory consistency, network/graph provider contract, stable smoke."
        };

        return new ValidationSuiteResult
        {
            Name = "Registry",
            Runs = 1,
            Failures = failures,
            Notes = notes
        };
    }
}
