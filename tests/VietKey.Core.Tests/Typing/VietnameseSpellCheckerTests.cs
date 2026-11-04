using VietKey.Core.Config;
using VietKey.Core.Typing;
using Xunit;

namespace VietKey.Core.Tests.Typing;

public sealed class VietnameseSpellCheckerTests
{
    private static string TypeSequence(VietnameseTransformer transformer, string keys, AppConfig? config = null)
    {
        var cfg = config ?? AppConfig.Default;
        var screen = new System.Text.StringBuilder();

        foreach (char c in keys)
        {
            var result = transformer.ProcessKey(c, cfg);
            if (!result.Handled)
            {
                screen.Append(c);
            }
            else
            {
                screen.Length -= result.BackspaceCount;
                screen.Append(result.ReplacementText);
            }
        }

        return screen.ToString();
    }

    // @covers REQ-typing-3
    [Theory]
    [InlineData("window ", "window ")]
    [InlineData("matf ", "matf ")]
    [InlineData("function ", "function ")]
    [InlineData("restore ", "restore ")]
    [InlineData("words ", "words ")]
    [InlineData("with ", "with ")]
    [InlineData("string ", "string ")]
    [InlineData("add ", "add ")]
    [InlineData("address ", "address ")]
    [InlineData("ddram ", "ddram ")]
    public void AutoRestoreEnglishAndInvalidPhonotactics_RestoresRawKeys(string input, string expected)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with
        {
            InputMethod = InputMethod.Telex,
            SpellCheck = true,
            AutoRestoreEnglish = true
        };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }

    // @covers REQ-typing-3
    [Theory]
    [InlineData("nghieengs ", "nghiếng ")]
    [InlineData("mats ", "mát ")]
    [InlineData("matj ", "mạt ")]
    [InlineData("truowngf ", "trường ")]
    [InlineData("khuyeechs ", "khuyếch ")]
    [InlineData("quynhf ", "quỳnh ")]
    [InlineData("giuwowngf ", "giường ")]
    [InlineData("xoawns ", "xoắn ")]
    [InlineData("ddc", "đc")]
    [InlineData("ddc ", "đc ")]
    [InlineData("dcd ", "đc ")]
    [InlineData("ddk ", "đk ")]
    [InlineData("ddt ", "đt ")]
    [InlineData("ddh ", "đh ")]
    [InlineData("ddv ", "đv ")]
    [InlineData("dd ", "đ ")]
    public void ValidVietnameseSyllables_ArePreservedWhenSpellCheckEnabled(string input, string expected)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with
        {
            InputMethod = InputMethod.Telex,
            SpellCheck = true,
            AutoRestoreEnglish = true
        };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }
}
