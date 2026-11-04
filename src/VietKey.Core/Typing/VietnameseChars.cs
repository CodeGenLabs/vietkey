namespace VietKey.Core.Typing;

public enum VowelHat
{
    None = 0,
    Breve = 1,      // ă (aw / 8)
    Circumflex = 2, // â, ê, ô (aa, ee, oo / 6)
    Horn = 3        // ơ, ư (ow, uw / 7)
}

public enum ToneMark
{
    None = 0,
    Sac = 1,   // s / 1 (á)
    Huyen = 2, // f / 2 (à)
    Hoi = 3,   // r / 3 (ả)
    Nga = 4,   // x / 4 (ã)
    Nang = 5   // j / 5 (ạ)
}

public static class VietnameseChars
{
    // Indexing: [baseVowelIndex, hatIndex, toneIndex]
    // baseVowel: a(0), e(1), i(2), o(3), u(4), y(5)
    private static readonly char[,] VowelTableLower = new char[12, 6]
    {
        // 0: a
        { 'a', 'á', 'à', 'ả', 'ã', 'ạ' },
        // 1: ă
        { 'ă', 'ắ', 'ằ', 'ẳ', 'ẵ', 'ặ' },
        // 2: â
        { 'â', 'ấ', 'ầ', 'ẩ', 'ẫ', 'ậ' },
        // 3: e
        { 'e', 'é', 'è', 'ẻ', 'ẽ', 'ẹ' },
        // 4: ê
        { 'ê', 'ế', 'ề', 'ể', 'ễ', 'ệ' },
        // 5: i
        { 'i', 'í', 'ì', 'ỉ', 'ĩ', 'ị' },
        // 6: o
        { 'o', 'ó', 'ò', 'ỏ', 'õ', 'ọ' },
        // 7: ô
        { 'ô', 'ố', 'ồ', 'ổ', 'ỗ', 'ộ' },
        // 8: ơ
        { 'ơ', 'ớ', 'ờ', 'ở', 'ỡ', 'ợ' },
        // 9: u
        { 'u', 'ú', 'ù', 'ủ', 'ũ', 'ụ' },
        // 10: ư
        { 'ư', 'ứ', 'ừ', 'ử', 'ữ', 'ự' },
        // 11: y
        { 'y', 'ý', 'ỳ', 'ỷ', 'ỹ', 'ỵ' }
    };

    private static readonly char[,] VowelTableUpper = new char[12, 6]
    {
        // 0: A
        { 'A', 'Á', 'À', 'Ả', 'Ã', 'Ạ' },
        // 1: Ă
        { 'Ă', 'Ắ', 'Ằ', 'Ẳ', 'Ẵ', 'Ặ' },
        // 2: Â
        { 'Â', 'Ấ', 'Ầ', 'Ẩ', 'Ẫ', 'Ậ' },
        // 3: E
        { 'E', 'É', 'È', 'Ẻ', 'Ẽ', 'Ẹ' },
        // 4: Ê
        { 'Ê', 'Ế', 'Ề', 'Ể', 'Ễ', 'Ệ' },
        // 5: I
        { 'I', 'Í', 'Ì', 'Ỉ', 'Ĩ', 'Ị' },
        // 6: O
        { 'O', 'Ó', 'Ò', 'Ỏ', 'Õ', 'Ọ' },
        // 7: Ô
        { 'Ô', 'Ố', 'Ồ', 'Ổ', 'Ỗ', 'Ộ' },
        // 8: Ơ
        { 'Ơ', 'Ớ', 'Ờ', 'Ở', 'Ỡ', 'Ợ' },
        // 9: U
        { 'U', 'Ú', 'Ù', 'Ủ', 'Ũ', 'Ụ' },
        // 10: Ư
        { 'Ư', 'Ứ', 'Ừ', 'Ử', 'Ữ', 'Ự' },
        // 11: Y
        { 'Y', 'Ý', 'Ỳ', 'Ỷ', 'Ỹ', 'Ỵ' }
    };

    public static bool IsBaseVowel(char c)
    {
        char lower = char.ToLowerInvariant(c);
        return lower is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';
    }

    public static char ComposeLetter(char baseChar, VowelHat hat, ToneMark tone, bool crossbarD = false)
    {
        bool upper = char.IsUpper(baseChar);
        char lower = char.ToLowerInvariant(baseChar);

        if (lower == 'd')
        {
            if (crossbarD)
            {
                return upper ? 'Đ' : 'đ';
            }
            return baseChar;
        }

        int row = (lower, hat) switch
        {
            ('a', VowelHat.None) => 0,
            ('a', VowelHat.Breve) => 1,
            ('a', VowelHat.Circumflex) => 2,
            ('e', VowelHat.None) => 3,
            ('e', VowelHat.Circumflex) => 4,
            ('i', _) => 5,
            ('o', VowelHat.None) => 6,
            ('o', VowelHat.Circumflex) => 7,
            ('o', VowelHat.Horn) => 8,
            ('u', VowelHat.None) => 9,
            ('u', VowelHat.Horn) => 10,
            ('y', _) => 11,
            _ => -1
        };

        if (row < 0)
        {
            return baseChar;
        }

        int col = (int)tone;
        return upper ? VowelTableUpper[row, col] : VowelTableLower[row, col];
    }
}
