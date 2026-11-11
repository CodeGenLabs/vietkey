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

    public LetterSlot Clone() => new(BaseChar)
    {
        Hat = Hat,
        CrossbarD = CrossbarD,
        HatSuppressed = HatSuppressed,
        CrossbarSuppressed = CrossbarSuppressed
    };
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

    public void RemoveLastRawKey()
    {
        if (_rawKeys.Count > 0)
        {
            _rawKeys.RemoveAt(_rawKeys.Count - 1);
        }
    }

    public void AddSlot(LetterSlot slot) => _slots.Add(slot);

    public bool HasAnyDiacritic()
    {
        if (Tone != ToneMark.None)
        {
            return true;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (s.Hat != VowelHat.None || s.CrossbarD)
            {
                return true;
            }
        }

        return false;
    }

    public string GetRawString() => new(_rawKeys.ToArray());

    public SyllableBuffer Clone()
    {
        var copy = new SyllableBuffer();
        copy.CopyFrom(this);
        return copy;
    }

    public void CopyFrom(SyllableBuffer source)
    {
        _slots.Clear();
        foreach (var slot in source._slots)
        {
            _slots.Add(slot.Clone());
        }
        _rawKeys.Clear();
        _rawKeys.AddRange(source._rawKeys);
        Tone = source.Tone;
        SuppressedTone = source.SuppressedTone;
        CompositionSuppressed = source.CompositionSuppressed;
        DisplayedText = source.DisplayedText;
    }

    /// <summary>Builds a buffer that mirrors literal text already on screen (no further composition).</summary>
    public static SyllableBuffer FromLiteral(string text)
    {
        var buffer = new SyllableBuffer();
        foreach (char c in text)
        {
            buffer._slots.Add(new LetterSlot(c));
            buffer._rawKeys.Add(c);
        }
        buffer.CompositionSuppressed = true;
        buffer.DisplayedText = text;
        return buffer;
    }
}
