using System.Text;

namespace VietKey.Core.Typing;

public static class VietnameseSpellChecker
{
    private static readonly HashSet<string> ValidInitials = new(StringComparer.Ordinal)
    {
        "", "b", "c", "ch", "d", "đ", "g", "gh", "gi", "h", "k", "kh",
        "l", "m", "n", "ng", "ngh", "nh", "p", "ph", "qu", "r", "s",
        "t", "th", "tr", "v", "x"
    };

    private static readonly HashSet<string> ValidFinals = new(StringComparer.Ordinal)
    {
        "", "c", "ch", "m", "n", "ng", "nh", "p", "t"
    };

    private static readonly HashSet<string> StopFinals = new(StringComparer.Ordinal)
    {
        "c", "ch", "p", "t"
    };

    private static readonly HashSet<string> ValidVowelClusters = new(StringComparer.Ordinal)
    {
        // Single vowels
        "a", "ă", "â", "e", "ê", "i", "o", "ô", "ơ", "u", "ư", "y",
        // Diphthongs
        "ai", "ao", "au", "ay", "âu", "ây",
        "eo", "êu",
        "ia", "iê", "ie", "iu",
        "oa", "oă", "oe", "oi", "ôi", "ơi", "oo",
        "ua", "uă", "uâ", "uê", "ui", "uô", "uo", "uơ", "uy",
        "ưa", "ưi", "ươ", "ưo", "ưu",
        "yê", "ye", "yu",
        // Triphthongs
        "iêu", "ieu", "oai", "oao", "oay", "oeo",
        "uây", "uôi", "uoi", "ươi", "ươu", "uou",
        "uya", "uyê", "uye", "uyu", "yêu", "yeu"
    };

    public static bool IsValidSyllable(SyllableBuffer buffer, bool isCompleteWord)
    {
        var slots = buffer.Slots;
        if (slots.Count == 0)
        {
            return true;
        }

        var initialBuilder = new StringBuilder(4);
        var vowelBuilder = new StringBuilder(4);
        var finalBuilder = new StringBuilder(4);

        int phase = 0; // 0 = initial, 1 = vowel, 2 = final

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            char lower = char.ToLowerInvariant(slot.BaseChar);
            if (!char.IsLetter(lower))
            {
                return false;
            }

            bool isVowel = IsEffectiveVowelAt(slots, i);

            if (phase == 0)
            {
                if (!isVowel)
                {
                    if (i > 0 && slot.CrossbarD)
                    {
                        return false;
                    }
                    initialBuilder.Append(slot.CrossbarD ? 'đ' : lower);
                }
                else
                {
                    phase = 1;
                    vowelBuilder.Append(VietnameseChars.ComposeLetter(lower, slot.Hat, ToneMark.None));
                }
            }
            else if (phase == 1)
            {
                if (isVowel)
                {
                    vowelBuilder.Append(VietnameseChars.ComposeLetter(lower, slot.Hat, ToneMark.None));
                }
                else
                {
                    if (slot.CrossbarD)
                    {
                        return false;
                    }
                    phase = 2;
                    finalBuilder.Append(lower);
                }
            }
            else
            {
                // phase == 2 (final consonant)
                if (isVowel || slot.CrossbarD)
                {
                    // A vowel after final consonants means multi-syllable/English word (e.g. "window", "restore")
                    return false;
                }
                finalBuilder.Append(lower);
            }
        }

        string initial = initialBuilder.ToString();
        string vowel = vowelBuilder.ToString();
        string final = finalBuilder.ToString();

        if (vowel.Length == 0)
        {
            // Preserve Vietnamese consonant-only shorthand starting with 'đ' (e.g. "đ", "đc", "đk", "đt", "đh", "đv")
            if (slots[0].CrossbarD && buffer.Tone == ToneMark.None)
            {
                return true;
            }

            if (!ValidInitials.Contains(initial))
            {
                return false;
            }

            return !isCompleteWord;
        }

        if (!ValidInitials.Contains(initial))
        {
            return false;
        }

        if (!ValidFinals.Contains(final))
        {
            return false;
        }

        if (vowel.Length > 3 || !ValidVowelClusters.Contains(vowel))
        {
            return false;
        }

        if (StopFinals.Contains(final))
        {
            if (buffer.Tone is ToneMark.Huyen or ToneMark.Hoi or ToneMark.Nga)
            {
                return false;
            }

            if (isCompleteWord && buffer.Tone == ToneMark.None)
            {
                return false;
            }
        }

        if (isCompleteWord && final.Length == 0 && vowel is "ă" or "â")
        {
            return false;
        }

        return true;
    }

    private static bool IsEffectiveVowelAt(IReadOnlyList<LetterSlot> slots, int index)
    {
        char lower = char.ToLowerInvariant(slots[index].BaseChar);
        if (!VietnameseChars.IsBaseVowel(lower))
        {
            return false;
        }

        if (lower == 'u' && index > 0 && char.ToLowerInvariant(slots[index - 1].BaseChar) == 'q')
        {
            if (index + 1 < slots.Count && VietnameseChars.IsBaseVowel(slots[index + 1].BaseChar))
            {
                return false;
            }
        }

        if (lower == 'i' && index > 0 && char.ToLowerInvariant(slots[index - 1].BaseChar) == 'g')
        {
            if (index + 1 < slots.Count && VietnameseChars.IsBaseVowel(slots[index + 1].BaseChar))
            {
                return false;
            }
        }

        return true;
    }
}
