using VietKey.Core.Config;
using VietKey.Core.Typing;
using Xunit;

namespace VietKey.Core.Tests.Typing;

public sealed class VietnameseTransformerTests
{
    private static string TypeSequence(VietnameseTransformer transformer, string keys, AppConfig? config = null)
    {
        var cfg = config ?? AppConfig.Default;
        var screen = new System.Text.StringBuilder();

        foreach (char c in keys)
        {
            if (c == '\b')
            {
                var bsResult = transformer.ProcessBackspace(cfg);
                if (!bsResult.Handled)
                {
                    if (screen.Length > 0)
                    {
                        screen.Length--;
                    }
                }
                else
                {
                    screen.Length -= bsResult.BackspaceCount;
                    screen.Append(bsResult.ReplacementText);
                }
                continue;
            }

            var result = transformer.ProcessKey(c, cfg);
            if (!result.Handled)
            {
                screen.Append(c);
            }
            else
            {
                Assert.True(result.BackspaceCount <= screen.Length,
                    $"BackspaceCount ({result.BackspaceCount}) exceeded screen length ({screen.Length}) on key '{c}'");
                screen.Length -= result.BackspaceCount;
                screen.Append(result.ReplacementText);
            }
        }

        return screen.ToString();
    }

    // @covers REQ-typing-1
    [Fact]
    public void Telex_ComposesTiengAndComputesMinimalDiff()
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with { InputMethod = InputMethod.Telex };

        string output = TypeSequence(transformer, "tieengs", cfg);
        Assert.Equal("tiếng", output);
    }

    // @covers REQ-typing-1
    [Theory]
    [InlineData("awa", "ăa")]
    [InlineData("aw", "ă")]
    [InlineData("aa", "â")]
    [InlineData("ee", "ê")]
    [InlineData("oo", "ô")]
    [InlineData("ow", "ơ")]
    [InlineData("uw", "ư")]
    [InlineData("dd", "đ")]
    [InlineData("DD", "Đ")]
    [InlineData("vieetj", "việt")]
    [InlineData("nguwowif", "người")]
    [InlineData("muwowif", "mười")]
    [InlineData("taas", "tấ")]
    [InlineData("tasz", "ta")]
    public void Telex_BasicVowelsCrossbarDAndTones_ComposeCorrectly(string input, string expected)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with { InputMethod = InputMethod.Telex, SpellCheck = false };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }

    // @covers REQ-typing-1
    [Theory]
    [InlineData("vie6t5", "việt")]
    [InlineData("tie6ng1", "tiếng")]
    [InlineData("d9u7o7ng2", "đường")]
    [InlineData("a8n", "ăn")]
    [InlineData("to6i", "tôi")]
    [InlineData("ta10", "ta")]
    public void Vni_ComposesVowelsCrossbarDAndTones(string input, string expected)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with { InputMethod = InputMethod.Vni, SpellCheck = false };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }

    // @covers REQ-typing-1
    [Theory]
    [InlineData("aaa", "aa", InputMethod.Telex)]
    [InlineData("aaaa", "aaa", InputMethod.Telex)]
    [InlineData("eee", "ee", InputMethod.Telex)]
    [InlineData("ooo", "oo", InputMethod.Telex)]
    [InlineData("ddd", "dd", InputMethod.Telex)]
    [InlineData("dddd", "ddd", InputMethod.Telex)]
    [InlineData("d99", "d9", InputMethod.Vni)]
    [InlineData("a66", "a6", InputMethod.Vni)]
    [InlineData("aww", "aw", InputMethod.Telex)]
    [InlineData("uww", "uw", InputMethod.Telex)]
    [InlineData("oww", "ow", InputMethod.Telex)]
    [InlineData("ass", "as", InputMethod.Telex)]
    [InlineData("asss", "ass", InputMethod.Telex)]
    public void DoubleModifierKeyUndo_RevertsAndSuppressesFurtherTransformInSyllable(
        string input, string expected, InputMethod method)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with { InputMethod = method, SpellCheck = false };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }

    // @covers REQ-typing-2
    [Theory]
    [InlineData("toanf", "toàn")]
    [InlineData("hoafn", "hoàn")]
    [InlineData("gifa", "già")]
    [InlineData("tuongwf", "tường")]
    [InlineData("duongwdf", "đường")]
    [InlineData("tuyetse", "tuyết")]
    [InlineData("muaw", "mưa")]
    [InlineData("chuaw", "chưa")]
    [InlineData("quawn", "quăn")]
    public void FreeMarkingAndDynamicToneRepositioning_WorksAcrossSyllable(string input, string expected)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with
        {
            InputMethod = InputMethod.Telex,
            ModernTone = false,
            SpellCheck = false
        };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }

    // @covers REQ-typing-2
    [Theory]
    [InlineData("hoaf", true, "hoà")]
    [InlineData("hoaf", false, "hòa")]
    [InlineData("khoer", true, "khoẻ")]
    [InlineData("khoer", false, "khỏe")]
    [InlineData("thuys", true, "thuý")]
    [InlineData("thuys", false, "thúy")]
    [InlineData("quys", true, "quý")]
    [InlineData("quys", false, "quý")]
    [InlineData("hoanf", true, "hoàn")]
    [InlineData("hoanf", false, "hoàn")]
    [InlineData("thuowr", true, "thuở")]
    [InlineData("thuowr", false, "thuở")]
    [InlineData("thuowngr", true, "thưởng")]
    public void TonePlacement_ModernVsClassic_PlacesToneOnExpectedVowel(
        string input, bool modernTone, string expected)
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with
        {
            InputMethod = InputMethod.Telex,
            ModernTone = modernTone,
            SpellCheck = false
        };

        string actual = TypeSequence(transformer, input, cfg);
        Assert.Equal(expected, actual);
    }
}
