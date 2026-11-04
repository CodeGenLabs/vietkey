namespace VietKey.Core.Typing;

public sealed class MacroTable
{
    private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Entries => _entries;

    public MacroTable(IReadOnlyDictionary<string, string>? initialEntries = null)
    {
        if (initialEntries is not null)
        {
            foreach (var kv in initialEntries)
            {
                _entries[kv.Key] = kv.Value;
            }
        }
    }

    public static bool TryExpand(
        string rawWord,
        string displayedWord,
        IReadOnlyDictionary<string, string>? macros,
        out string expanded)
    {
        expanded = string.Empty;
        if (macros is null || macros.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(rawWord) && macros.TryGetValue(rawWord, out string? rawMatch) && !string.IsNullOrEmpty(rawMatch))
        {
            expanded = rawMatch;
            return true;
        }

        if (!string.IsNullOrEmpty(displayedWord) && macros.TryGetValue(displayedWord, out string? dispMatch) && !string.IsNullOrEmpty(dispMatch))
        {
            expanded = dispMatch;
            return true;
        }

        return false;
    }
}
