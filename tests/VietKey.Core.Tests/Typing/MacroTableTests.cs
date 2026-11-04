using VietKey.Core.Config;
using VietKey.Core.Typing;
using Xunit;

namespace VietKey.Core.Tests.Typing;

public sealed class MacroTableTests
{
    private static string TypeSequence(
        VietnameseTransformer transformer,
        string keys,
        AppConfig config,
        IReadOnlyDictionary<string, string> macros)
    {
        var screen = new System.Text.StringBuilder();

        foreach (char c in keys)
        {
            var result = transformer.ProcessKey(c, config, macros);
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

    // @covers REQ-typing-4
    [Fact]
    public void WordBoundary_WhenMacroMatchesOnSpaceOrEnter_ExpandsTriggerText()
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with { EnableMacros = true };
        var macros = new Dictionary<string, string>
        {
            ["vn"] = "Việt Nam",
            ["hcm"] = "Thành phố Hồ Chí Minh"
        };

        string actualSpace = TypeSequence(transformer, "vn ", cfg, macros);
        Assert.Equal("Việt Nam ", actualSpace);

        string actualEnter = TypeSequence(transformer, "hcm\n", cfg, macros);
        Assert.Equal("Thành phố Hồ Chí Minh\n", actualEnter);
    }

    // @covers REQ-typing-4
    [Fact]
    public void WordBoundary_WhenMacroDoesNotMatchOrDisabled_EmitsNormalText()
    {
        var transformer = new VietnameseTransformer();
        var cfg = AppConfig.Default with { EnableMacros = true, SpellCheck = false };
        var macros = new Dictionary<string, string>
        {
            ["vn"] = "Việt Nam"
        };

        string nonMatching = TypeSequence(transformer, "vna ", cfg, macros);
        Assert.Equal("vna ", nonMatching);

        var disabledCfg = cfg with { EnableMacros = false };
        string whenDisabled = TypeSequence(transformer, "vn ", disabledCfg, macros);
        Assert.Equal("vn ", whenDisabled);
    }
}
