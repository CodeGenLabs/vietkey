using System.Text;
using VietKey.Core.Config;

namespace VietKey.Core.Typing;

public sealed class VietnameseTransformer
{
    private readonly SyllableBuffer _buffer = new();

    public SyllableBuffer Buffer => _buffer;
    public string CurrentDisplayedText => _buffer.DisplayedText;

    public void Reset() => _buffer.Clear();

    public KeyTransformResult ProcessKey(char ch, AppConfig config, IReadOnlyDictionary<string, string>? macros = null)
    {
        if (!config.VietnameseEnabled)
        {
            _buffer.Clear();
            return KeyTransformResult.PassThrough();
        }

        if (IsWordBoundary(ch))
        {
            var boundaryResult = HandleWordBoundary(ch, config, macros);
            _buffer.Clear();
            return boundaryResult;
        }

        string previousDisplayed = _buffer.DisplayedText;
        _buffer.AddRawKey(ch);

        if (_buffer.CompositionSuppressed)
        {
            _buffer.AddSlot(new LetterSlot(ch));
            string rawOut = _buffer.DisplayedText + ch;
            _buffer.DisplayedText = rawOut;
            return KeyTransformResult.PassThrough(rawOut);
        }

        bool modified = config.InputMethod == InputMethod.Telex
            ? TryApplyTelex(ch)
            : TryApplyVni(ch);

        if (!modified)
        {
            _buffer.AddSlot(new LetterSlot(ch));
            RebalanceUoHornCluster();
        }

        string newDisplayed = RenderCurrentSyllable(config);

        if (config.SpellCheck && config.AutoRestoreEnglish &&
            _buffer.HasAnyDiacritic() &&
            !string.Equals(newDisplayed, _buffer.GetRawString(), StringComparison.Ordinal) &&
            ShouldAutoRestoreOnKeystroke(config))
        {
            newDisplayed = _buffer.GetRawString();
            _buffer.CompositionSuppressed = true;
        }

        _buffer.DisplayedText = newDisplayed;
        return ComputeDiff(previousDisplayed, newDisplayed, ch);
    }

    public KeyTransformResult ProcessBackspace(AppConfig config)
    {
        if (_buffer.IsEmpty)
        {
            return KeyTransformResult.PassThrough();
        }

        if (_buffer.DisplayedText.Length <= 1)
        {
            _buffer.Clear();
            return KeyTransformResult.PassThrough();
        }

        // Remove last displayed character slot and re-render
        string prev = _buffer.DisplayedText;
        if (_buffer.Slots.Count > 0)
        {
            var slotsList = (List<LetterSlot>)_buffer.Slots;
            slotsList.RemoveAt(slotsList.Count - 1);
        }
        _buffer.RemoveLastRawKey();

        if (_buffer.Slots.Count == 0)
        {
            _buffer.Clear();
            return KeyTransformResult.PassThrough();
        }

        string updated = _buffer.CompositionSuppressed
            ? prev[..^1]
            : RenderCurrentSyllable(config);
        _buffer.DisplayedText = updated;

        if (updated == prev[..^1])
        {
            return KeyTransformResult.PassThrough(updated);
        }

        // Replacing previous string (minus the physical backspace if we handle it)
        return new KeyTransformResult(true, prev.Length, updated, updated);
    }

    private bool TryApplyTelex(char ch)
    {
        char lower = char.ToLowerInvariant(ch);

        // 1. Crossbar D: d + d -> đ (or D + D -> Đ)
        if (lower == 'd')
        {
            int dIndex = FindModifiableDIndex();
            if (dIndex >= 0)
            {
                var slot = _buffer.Slots[dIndex];
                if (slot.CrossbarD)
                {
                    // Undo đ -> dd
                    slot.CrossbarD = false;
                    slot.CrossbarSuppressed = true;
                    _buffer.AddSlot(new LetterSlot(ch) { CrossbarSuppressed = true });
                    _buffer.RemoveLastRawKey();
                    _buffer.CompositionSuppressed = true;
                    return true;
                }
                if (!slot.CrossbarSuppressed)
                {
                    slot.CrossbarD = true;
                    return true;
                }
            }
            return false;
        }

        // 2. Circumflex: aa -> â, ee -> ê, oo -> ô
        if (lower is 'a' or 'e' or 'o')
        {
            int vIndex = FindLastBaseVowelIndex(lower);
            if (vIndex >= 0)
            {
                var slot = _buffer.Slots[vIndex];
                if (slot.Hat == VowelHat.Circumflex)
                {
                    // Double-key undo: â + a -> aa
                    slot.Hat = VowelHat.None;
                    slot.HatSuppressed = true;
                    _buffer.AddSlot(new LetterSlot(ch) { HatSuppressed = true });
                    _buffer.RemoveLastRawKey();
                    return true;
                }
                if (!slot.HatSuppressed && slot.Hat == VowelHat.None)
                {
                    slot.Hat = VowelHat.Circumflex;
                    return true;
                }
            }
            return false;
        }

        // 3. Horn / Breve: w -> ă, ơ, ư (or uo + w -> ươ)
        if (lower == 'w')
        {
            if (TryApplyTelexW(ch))
            {
                return true;
            }
            return false;
        }

        // 4. Tone marks: s, f, r, x, j, z
        ToneMark? tone = lower switch
        {
            's' => ToneMark.Sac,
            'f' => ToneMark.Huyen,
            'r' => ToneMark.Hoi,
            'x' => ToneMark.Nga,
            'j' => ToneMark.Nang,
            'z' => ToneMark.None,
            _ => null
        };

        if (tone.HasValue && HasAnyVowel())
        {
            return TryApplyTone(tone.Value, ch);
        }

        return false;
    }

    private bool TryApplyTelexW(char rawW)
    {
        // Check for undo on last modified w vowel (ă, ơ, ư) if w is pressed immediately again
        for (int i = _buffer.Slots.Count - 1; i >= 0; i--)
        {
            var s = _buffer.Slots[i];
            char b = char.ToLowerInvariant(s.BaseChar);
            if (b is 'a' or 'o' or 'u')
            {
                if (s.Hat is VowelHat.Breve or VowelHat.Horn)
                {
                    // Check if previous slot is also 'u' with Horn ('ươ' undo -> 'uow')
                    if (b == 'o' && i > 0 && char.ToLowerInvariant(_buffer.Slots[i - 1].BaseChar) == 'u'
                        && _buffer.Slots[i - 1].Hat == VowelHat.Horn)
                    {
                        _buffer.Slots[i - 1].Hat = VowelHat.None;
                        _buffer.Slots[i - 1].HatSuppressed = true;
                    }
                    s.Hat = VowelHat.None;
                    s.HatSuppressed = true;
                    _buffer.AddSlot(new LetterSlot(rawW));
                    _buffer.RemoveLastRawKey();
                    _buffer.CompositionSuppressed = true;
                    return true;
                }
                break;
            }
        }

        // Check for 'uo' or 'ưo' cluster -> 'ươ'
        for (int i = _buffer.Slots.Count - 1; i >= 1; i--)
        {
            char curr = char.ToLowerInvariant(_buffer.Slots[i].BaseChar);
            char prev = char.ToLowerInvariant(_buffer.Slots[i - 1].BaseChar);
            bool prevIsQu = i >= 2 && char.ToLowerInvariant(_buffer.Slots[i - 2].BaseChar) == 'q' && prev == 'u';

            if (curr == 'o' && prev == 'u' && !_buffer.Slots[i].HatSuppressed)
            {
                bool hasTrailingSlot = i + 1 < _buffer.Slots.Count;
                if (!prevIsQu && (hasTrailingSlot || _buffer.Slots[i - 1].Hat == VowelHat.Horn))
                {
                    _buffer.Slots[i - 1].Hat = VowelHat.Horn;
                }
                _buffer.Slots[i].Hat = VowelHat.Horn;
                return true;
            }
        }

        // Check for 'u' + 'a' after 'q' (qua + w -> quă) or 'u' + 'a' (ua + w -> ưa)
        for (int i = _buffer.Slots.Count - 1; i >= 0; i--)
        {
            var slot = _buffer.Slots[i];
            if (slot.HatSuppressed)
            {
                continue;
            }

            char b = char.ToLowerInvariant(slot.BaseChar);
            if (b == 'a')
            {
                if (i > 0 && char.ToLowerInvariant(_buffer.Slots[i - 1].BaseChar) == 'u')
                {
                    bool isQu = i >= 2 && char.ToLowerInvariant(_buffer.Slots[i - 2].BaseChar) == 'q';
                    if (!isQu && !_buffer.Slots[i - 1].HatSuppressed)
                    {
                        _buffer.Slots[i - 1].Hat = VowelHat.Horn;
                        return true;
                    }
                }
                slot.Hat = VowelHat.Breve;
                return true;
            }
            if (b == 'o')
            {
                slot.Hat = VowelHat.Horn;
                return true;
            }
            if (b == 'u')
            {
                // Don't horn 'u' in 'qu' unless it's the only vowel
                bool isQu = i > 0 && char.ToLowerInvariant(_buffer.Slots[i - 1].BaseChar) == 'q';
                if (!isQu)
                {
                    slot.Hat = VowelHat.Horn;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryApplyTone(ToneMark requestedTone, char rawChar)
    {
        if (requestedTone == ToneMark.None)
        {
            // 'z' or '0' removes existing tone
            if (_buffer.Tone != ToneMark.None)
            {
                _buffer.Tone = ToneMark.None;
                return true;
            }
            return false;
        }

        if (_buffer.SuppressedTone == requestedTone)
        {
            return false;
        }

        if (_buffer.Tone == requestedTone)
        {
            // Double tone key undo: e.g. 't' + 'e' + 's' + 's' -> "tes"
            _buffer.Tone = ToneMark.None;
            _buffer.SuppressedTone = requestedTone;
            _buffer.AddSlot(new LetterSlot(rawChar));
            _buffer.RemoveLastRawKey();
            _buffer.CompositionSuppressed = true;
            return true;
        }

        _buffer.Tone = requestedTone;
        return true;
    }

    private bool TryApplyVni(char ch)
    {
        if (ch is < '0' or > '9')
        {
            return false;
        }

        if (ch == '9')
        {
            int dIndex = FindModifiableDIndex();
            if (dIndex >= 0)
            {
                var slot = _buffer.Slots[dIndex];
                if (slot.CrossbarD)
                {
                    slot.CrossbarD = false;
                    slot.CrossbarSuppressed = true;
                    _buffer.AddSlot(new LetterSlot(ch));
                    _buffer.RemoveLastRawKey();
                    _buffer.CompositionSuppressed = true;
                    return true;
                }
                if (!slot.CrossbarSuppressed)
                {
                    slot.CrossbarD = true;
                    return true;
                }
            }
            return false;
        }

        if (ch == '6')
        {
            for (int i = _buffer.Slots.Count - 1; i >= 0; i--)
            {
                var slot = _buffer.Slots[i];
                char b = char.ToLowerInvariant(slot.BaseChar);
                if (b is 'a' or 'e' or 'o')
                {
                    if (slot.Hat == VowelHat.Circumflex)
                    {
                        slot.Hat = VowelHat.None;
                        slot.HatSuppressed = true;
                        _buffer.AddSlot(new LetterSlot(ch));
                        _buffer.RemoveLastRawKey();
                        _buffer.CompositionSuppressed = true;
                        return true;
                    }
                    if (!slot.HatSuppressed)
                    {
                        slot.Hat = VowelHat.Circumflex;
                        return true;
                    }
                }
            }
            return false;
        }

        if (ch == '8')
        {
            int aIdx = FindLastBaseVowelIndex('a');
            if (aIdx >= 0)
            {
                var slot = _buffer.Slots[aIdx];
                if (slot.Hat == VowelHat.Breve)
                {
                    slot.Hat = VowelHat.None;
                    slot.HatSuppressed = true;
                    _buffer.AddSlot(new LetterSlot(ch));
                    _buffer.RemoveLastRawKey();
                    _buffer.CompositionSuppressed = true;
                    return true;
                }
                if (!slot.HatSuppressed)
                {
                    slot.Hat = VowelHat.Breve;
                    return true;
                }
            }
            return false;
        }

        if (ch == '7')
        {
            return TryApplyTelexW(ch);
        }

        ToneMark? tone = ch switch
        {
            '1' => ToneMark.Sac,
            '2' => ToneMark.Huyen,
            '3' => ToneMark.Hoi,
            '4' => ToneMark.Nga,
            '5' => ToneMark.Nang,
            '0' => ToneMark.None,
            _ => null
        };

        if (tone.HasValue && HasAnyVowel())
        {
            return TryApplyTone(tone.Value, ch);
        }

        return false;
    }

    private void RebalanceUoHornCluster()
    {
        // When user types "u", "w" (-> "ư"), "o" (-> "ưo"), "i" (-> "ươi"),
        // or "t", "h", "u", "o", "w" (-> "thuơ") followed by "n" (-> "thươn")
        for (int i = 1; i < _buffer.Slots.Count; i++)
        {
            var prev = _buffer.Slots[i - 1];
            var curr = _buffer.Slots[i];
            if (char.ToLowerInvariant(prev.BaseChar) == 'u' &&
                char.ToLowerInvariant(curr.BaseChar) == 'o' &&
                i + 1 < _buffer.Slots.Count)
            {
                bool prevIsQu = i >= 2 && char.ToLowerInvariant(_buffer.Slots[i - 2].BaseChar) == 'q';
                if (prev.Hat == VowelHat.Horn && curr.Hat == VowelHat.None && !curr.HatSuppressed)
                {
                    curr.Hat = VowelHat.Horn;
                }
                else if (!prevIsQu && prev.Hat == VowelHat.None && !prev.HatSuppressed && curr.Hat == VowelHat.Horn)
                {
                    prev.Hat = VowelHat.Horn;
                }
            }
        }
    }

    private int FindModifiableDIndex()
    {
        if (_buffer.Slots.Count == 0)
        {
            return -1;
        }

        // Either the immediately preceding slot is 'd', or slot 0 is 'd' (free marking at end of syllable)
        int last = _buffer.Slots.Count - 1;
        if (char.ToLowerInvariant(_buffer.Slots[last].BaseChar) == 'd')
        {
            return last;
        }

        if (char.ToLowerInvariant(_buffer.Slots[0].BaseChar) == 'd')
        {
            return 0;
        }

        return -1;
    }

    private int FindLastBaseVowelIndex(char targetLowerVowel)
    {
        for (int i = _buffer.Slots.Count - 1; i >= 0; i--)
        {
            if (char.ToLowerInvariant(_buffer.Slots[i].BaseChar) == targetLowerVowel)
            {
                return i;
            }
        }
        return -1;
    }

    private bool HasAnyVowel()
    {
        for (int i = 0; i < _buffer.Slots.Count; i++)
        {
            if (IsEffectiveVowel(i))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsEffectiveVowel(int index)
    {
        var slot = _buffer.Slots[index];
        char lower = char.ToLowerInvariant(slot.BaseChar);
        if (!VietnameseChars.IsBaseVowel(lower))
        {
            return false;
        }

        // 'u' after 'q' is part of initial consonant 'qu' when another vowel follows
        if (lower == 'u' && index > 0 && char.ToLowerInvariant(_buffer.Slots[index - 1].BaseChar) == 'q')
        {
            if (index + 1 < _buffer.Slots.Count && VietnameseChars.IsBaseVowel(_buffer.Slots[index + 1].BaseChar))
            {
                return false;
            }
        }

        // 'i' after 'g' is part of initial consonant 'gi' when another vowel follows
        if (lower == 'i' && index > 0 && char.ToLowerInvariant(_buffer.Slots[index - 1].BaseChar) == 'g')
        {
            if (index + 1 < _buffer.Slots.Count && VietnameseChars.IsBaseVowel(_buffer.Slots[index + 1].BaseChar))
            {
                return false;
            }
        }

        return true;
    }

    private string RenderCurrentSyllable(AppConfig config)
    {
        int count = _buffer.Slots.Count;
        if (count == 0)
        {
            return string.Empty;
        }

        int toneTargetIndex = FindToneTargetVowelIndex(config.ModernTone);
        var sb = new StringBuilder(count);

        for (int i = 0; i < count; i++)
        {
            var slot = _buffer.Slots[i];
            ToneMark tone = (i == toneTargetIndex) ? _buffer.Tone : ToneMark.None;
            sb.Append(VietnameseChars.ComposeLetter(slot.BaseChar, slot.Hat, tone, slot.CrossbarD));
        }

        return sb.ToString();
    }

    private int FindToneTargetVowelIndex(bool modernTone)
    {
        if (_buffer.Tone == ToneMark.None)
        {
            return -1;
        }

        var vowelIndices = new List<int>(4);
        for (int i = 0; i < _buffer.Slots.Count; i++)
        {
            if (IsEffectiveVowel(i))
            {
                vowelIndices.Add(i);
            }
        }

        if (vowelIndices.Count == 0)
        {
            return -1;
        }

        if (vowelIndices.Count == 1)
        {
            return vowelIndices[0];
        }

        // If any vowel has a hat (ă, â, ê, ô, ơ, ư), prioritize it:
        // In "ươ" cluster, the tone goes on 'ơ' (the second horned vowel)
        int hornedO = -1;
        int anyHatted = -1;
        foreach (int idx in vowelIndices)
        {
            var slot = _buffer.Slots[idx];
            if (slot.Hat != VowelHat.None)
            {
                if (char.ToLowerInvariant(slot.BaseChar) == 'o' && slot.Hat == VowelHat.Horn)
                {
                    hornedO = idx;
                }
                anyHatted = idx;
            }
        }

        if (hornedO >= 0)
        {
            return hornedO;
        }

        if (anyHatted >= 0)
        {
            return anyHatted;
        }

        bool hasFinalConsonant = vowelIndices[^1] < _buffer.Slots.Count - 1;

        if (vowelIndices.Count == 2)
        {
            if (hasFinalConsonant)
            {
                return vowelIndices[1];
            }

            string pair = string.Concat(
                char.ToLowerInvariant(_buffer.Slots[vowelIndices[0]].BaseChar),
                char.ToLowerInvariant(_buffer.Slots[vowelIndices[1]].BaseChar));

            if (pair is "oa" or "oe" or "uy")
            {
                return modernTone ? vowelIndices[1] : vowelIndices[0];
            }

            return vowelIndices[0];
        }

        // 3 vowels (e.g. oai, uya, uye): middle vowel gets the tone
        return vowelIndices[1];
    }

    private bool ShouldAutoRestoreOnKeystroke(AppConfig config)
    {
        if (!VietnameseSpellChecker.IsValidSyllable(_buffer, isCompleteWord: false))
        {
            return true;
        }
        return false;
    }

    private KeyTransformResult HandleWordBoundary(char delimiter, AppConfig config, IReadOnlyDictionary<string, string>? macros)
    {
        if (_buffer.IsEmpty)
        {
            return KeyTransformResult.PassThrough();
        }

        string currentDisplayed = _buffer.DisplayedText;
        string raw = _buffer.GetRawString();

        if (config.EnableMacros && MacroTable.TryExpand(raw, currentDisplayed, macros, out string expanded))
        {
            string expandedWithDelim = expanded + delimiter;
            return new KeyTransformResult(true, currentDisplayed.Length, expandedWithDelim, expandedWithDelim);
        }

        if (config.SpellCheck && config.AutoRestoreEnglish &&
            _buffer.HasAnyDiacritic() &&
            !string.Equals(currentDisplayed, raw, StringComparison.Ordinal) &&
            !VietnameseSpellChecker.IsValidSyllable(_buffer, isCompleteWord: true))
        {
            string restoredWithDelim = raw + delimiter;
            return ComputeDiff(currentDisplayed, restoredWithDelim, delimiter);
        }

        return KeyTransformResult.PassThrough();
    }

    private static bool IsWordBoundary(char ch) =>
        !char.IsLetterOrDigit(ch);

    private static KeyTransformResult ComputeDiff(string previousDisplayed, string newDisplayed, char physicalChar)
    {
        if (newDisplayed.Length == previousDisplayed.Length + 1 &&
            newDisplayed.StartsWith(previousDisplayed, StringComparison.Ordinal) &&
            newDisplayed[^1] == physicalChar)
        {
            return KeyTransformResult.PassThrough(newDisplayed);
        }

        int commonPrefix = 0;
        int minLen = Math.Min(previousDisplayed.Length, newDisplayed.Length);
        while (commonPrefix < minLen && previousDisplayed[commonPrefix] == newDisplayed[commonPrefix])
        {
            commonPrefix++;
        }

        int backspaces = previousDisplayed.Length - commonPrefix;
        string replacement = newDisplayed[commonPrefix..];
        return new KeyTransformResult(true, backspaces, replacement, newDisplayed);
    }
}
