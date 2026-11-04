namespace VietKey.Core.Typing;

public readonly record struct KeyTransformResult(
    bool Handled,
    int BackspaceCount,
    string ReplacementText,
    string ComposedWord)
{
    public static KeyTransformResult PassThrough(string currentWord = "") =>
        new(false, 0, string.Empty, currentWord);
}
