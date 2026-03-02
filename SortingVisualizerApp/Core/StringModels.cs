namespace SortingVisualizerApp.Core;

public enum StringAlphabetSet
{
    Digits,
    Lowercase,
    Uppercase,
    Mixed
}

public enum StringDistributionPreset
{
    Random,
    CommonPrefix,
    ManyDuplicates
}

public readonly record struct StringItem(
    int Id,
    string Text,
    int OriginalIndex);

public readonly record struct StringSortOptions(
    StringAlphabetSet Alphabet,
    bool EmitExtendedEvents = true);

public interface IStringSortAlgorithm
{
    IEnumerable<SortEvent> Execute(StringItem[] data, StringSortOptions options);
}
