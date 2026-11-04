namespace VietKey.Core.Typing;

public sealed class LetterSlot
{
    public char BaseChar { get; set; }
    public VowelHat Hat { get; set; }
    public bool CrossbarD { get; set; }
    public bool HatSuppressed { get; set; }
    public bool CrossbarSuppressed { get; set; }

    public LetterSlot(char baseChar)
    {
        BaseChar = baseChar;
    }
}

public sealed class SyllableBuffer
{
    private readonly List<LetterSlot> _slots = new(16);
    private readonly List<char> _rawKeys = new(24);

    public IReadOnlyList<LetterSlot> Slots => _slots;
    public IReadOnlyList<char> RawKeys => _rawKeys;
    public ToneMark Tone { get; set; } = ToneMark.None;
    public ToneMark? SuppressedTone { get; set; }
    public bool CompositionSuppressed { get; set; }
    public string DisplayedText { get; set; } = string.Empty;

    public bool IsEmpty => _rawKeys.Count == 0;

    public void Clear()
    {
        _slots.Clear();
        _rawKeys.Clear();
        Tone = ToneMark.None;
        SuppressedTone = null;
        CompositionSuppressed = false;
        DisplayedText = string.Empty;
    }

    public void AddRawKey(char key) => _rawKeys.Add(key);

    public void AddSlot(LetterSlot slot) => _slots.Add(slot);

    public string GetRawString() => new(_rawKeys.ToArray());
}
