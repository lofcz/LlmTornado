using System;
using System.Threading.Tasks;

namespace LlmTornado.Code;

/*
 *  With minor modifications and removed shims for old .NET runtimes taken from
 *  https://github.com/codeyu/nanoid-net
 *  MIT licensed
 */

/// <summary>
/// Static class containing all functions and constants related to Nanoid.
/// </summary>
internal static class Nanoid
{
    /// <summary>
    /// Useful alphabets for Nanoid generation.
    /// Taken from https://github.com/CyberAP/nanoid-dictionary
    /// and https://github.com/SasLuca/zig-nanoid/blob/91e0a9a8890984f3dcdd98c99002a05a83d0ee89/src/nanoid.zig#L4.
    /// </summary>
    public static class Alphabets
    {
        /// <summary>
        /// Used for composition and documentation in building proper Alphabets.
        /// Not recommended to be used on their own as alphabets for Nanoid.
        /// </summary>
        public static class SubAlphabets
        {
            /// <summary>
            /// All digits that don't look similar to other digits or letters.
            /// </summary>
            public const string NoLookAlikeDigits = "346789";

            /// <summary>
            /// All lowercase letters that don't look similar to other letters.
            /// </summary>
            public const string NoLookAlikeLettersLowercase = "abcdefghijkmnpqrtwxyz";

            /// <summary>
            /// All uppercase letters that don't look similar to other letters.
            /// </summary>
            public const string NoLookAlikeLettersUppercase = "ABCDEFGHJKLMNPQRTUVWXY";

            /// <summary>
            /// All letters that don't look similar to other letters.
            /// </summary>
            public const string NoLookAlikeLetters = NoLookAlikeLettersUppercase + NoLookAlikeLettersLowercase;

            /// <summary>
            /// All digits that don't look similar to other digits or letters
            /// and prevent potential obscene words from appearing in generated ids.
            /// </summary>
            public const string NoLookAlikeSafeDigits = "6789";

            /// <summary>
            /// All lowercase letters that don't look similar to other digits or letters
            /// and prevent potential obscene words from appearing in generated ids.
            /// </summary>
            public const string NoLookAlikeSafeLettersLowercase = "bcdfghjkmnpqrtwz";

            /// <summary>
            /// All uppercase letters that don't look similar to other digits or letters
            /// and prevent potential obscene words from appearing in generated ids.
            /// </summary>
            public const string NoLookAlikeSafeLettersUppercase = "BCDFGHJKLMNPQRTW";

            /// <summary>
            /// All letters that don't look similar to other digits or letters
            /// and prevent potential obscene words from appearing in generated ids.
            /// </summary>
            public const string NoLookAlikeSafeLetters = NoLookAlikeLettersUppercase + NoLookAlikeLettersLowercase;

            /// <summary>
            /// URL safe symbols that can be used in a Nanoid. Part of the default alphabet.
            /// </summary>
            public const string Symbols = "_-";
        }

        /// <summary>
        /// All digits [0, 9].
        /// </summary>
        public const string Digits = "0123456789";

        /// <summary>
        /// English hexadecimal with lowercase characters.
        /// </summary>
        public const string HexadecimalLowercase = Digits + "abcdef";

        /// <summary>
        /// English hexadecimal with uppercase characters.
        /// </summary>
        public const string HexadecimalUppercase = Digits + "ABCDEF";

        /// <summary>
        /// Lowercase English alphabet letters.
        /// </summary>
        public const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Uppercase English alphabet letters.
        /// </summary>
        public const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Lowercase and uppercase English alphabet letters.
        /// </summary>
        public const string Letters = LowercaseLetters + UppercaseLetters;

        /// <summary>
        /// Lowercase English alphabet letters and digits.
        /// </summary>
        public const string LowercaseLettersAndDigits = Digits + LowercaseLetters;

        /// <summary>
        /// Uppercase English alphabet letters and digits.
        /// </summary>
        public const string UppercaseLettersAndDigits = Digits + UppercaseLetters;

        /// <summary>
        /// English alphabet letters and digits.
        /// </summary>
        public const string LettersAndDigits = Digits + LowercaseLetters + UppercaseLetters;

        /// <summary>
        /// English alphabet letters and digits without lookalikes: 1, l, I, 0, O, o, u, v, 5, S, s, 2, Z.
        /// </summary>
        public const string NoLookAlikes = SubAlphabets.NoLookAlikeDigits + SubAlphabets.NoLookAlikeLetters;

        /// <summary>
        /// English alphabet letters and digits without lookalikes (1, l, I, 0, O, o, u, v, 5, S, s, 2, Z)
        /// and with removed vowels and the following letters: 3, 4, x, X, V.
        /// This list should protect you from accidentally getting obscene words in generated strings.
        /// </summary>
        public const string NoLookAlikesSafe = SubAlphabets.NoLookAlikeSafeDigits + SubAlphabets.NoLookAlikeSafeLetters;

        /// <summary>
        /// The default alphabet used by Nanoid. Includes ascii digits, letters and the symbols '_' and '-'.
        /// </summary>
        public const string Default = SubAlphabets.Symbols + LettersAndDigits;
    }

    /// <summary>
    /// The default size of a Nanoid.
    /// </summary>
    public const int DefaultIdSize = 21;

    /// <summary>
    /// Backing field for <see cref="Nanoid.GlobalRandom"/>.
    /// </summary>
    [ThreadStatic]
    private static CryptoRandom? _globalRandom;

    /// <summary>
    /// Global <see cref="CryptoRandom"/> instance used to conveniently generate Nanoids.
    /// </summary>
    /// <remarks>
    /// Lazily initialized in order to account for the ThreadStatic attribute (learn more here: https://stackoverflow.com/a/18086509)
    /// </remarks>
    public static CryptoRandom GlobalRandom => _globalRandom ??= new CryptoRandom();

    /// <summary>
    /// Generate a Nanoid using a global instance of <see cref="CryptoRandom"/>.
    /// </summary>
    /// <param name="alphabet">The set of characters used in generating the id. Defaults to <see cref="Alphabets.Default">Alphabets.Default.</see></param>
    /// <param name="size">The length of the id. Defaults to <see cref="DefaultIdSize"/>.</param>
    /// <returns>A new string representing a random nanoid with the specified <paramref name="alphabet"/> and <paramref name="size"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the provided arguments are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="alphabet"/>'s length is outside the range [0, 256] or if <paramref name="size"/> is less than or equal to 0.</exception>
    public static string Generate(string alphabet = Alphabets.Default, int size = 21)
    {
        Validate(alphabet, size);
        return GenerateImpl(GlobalRandom, alphabet, size);
    }
    
    /// <summary>
    /// Generate a Nanoid.
    /// </summary>
    /// <param name="random">A random number generator.</param>
    /// <param name="alphabet">The set of characters used in generating the id. Defaults to <see cref="Alphabets.Default">Alphabets.Default.</see></param>
    /// <param name="size">The length of the id. Defaults to <see cref="DefaultIdSize"/>.</param>
    /// <returns>A new string representing a random nanoid with the specified <paramref name="alphabet"/> and <paramref name="size"/>.</returns>
    /// <exception cref="ArgumentNullException">If any of the provided arguments are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="alphabet"/>'s length is outside the range [0, 256] or if <paramref name="size"/> is less than or equal to 0.</exception>
    public static string Generate(Random random, string alphabet = Alphabets.Default, int size = 21)
    {
        Validate(alphabet, size);
        return GenerateImpl(random, alphabet, size);
    }

    private static void Validate(string alphabet, int size)
    {
        if (alphabet == null)
        {
            throw new ArgumentNullException(nameof(alphabet), "alphabet cannot be null.");
        }

        if (alphabet.Length <= 0 || alphabet.Length >= 256)
        {
            throw new ArgumentOutOfRangeException(nameof(alphabet), "alphabet must contain between 1 and 255 symbols.");
        }

        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "size must be greater than zero.");
        }
    }

    /// <summary>
    /// Internal implementation of the nanoid algorithm.
    /// </summary>
    /// <remarks>
    /// To be called by public facing <c>Generate</c> functions.
    /// </remarks>
    private static string GenerateImpl(Random random, string alphabet = Alphabets.Default, int size = 21)
    {
        // See https://github.com/ai/nanoid/blob/master/format.js for an
        // explanation as to why masking with `random % alphabet` is a common
        // mistake security-wise.

        // Use `Int32.LeadingZeroCount` on .net7 and above
      
        int mask = (2 << 31 - int.LeadingZeroCount(alphabet.Length - 1 | 1)) - 1;
        
        // Original dev notes regarding this algorithm.
        // Source: https://github.com/ai/nanoid/blob/0454333dee4612d2c2e163d271af6cc3ce1e5aa4/index.js#L45
        //
        // "Next, a step determines how many random bytes to generate.
        // The number of random bytes gets decided upon the ID length, mask,
        // alphabet length, and magic number 1.6 (using 1.6 peaks at performance
        // according to benchmarks)."
        int step = (int) Math.Ceiling(1.6 * mask * size / alphabet.Length);
        Span<char> idBuilder = stackalloc char[size];
        Span<byte> bytes = stackalloc byte[step];

        int cnt = 0;

        while (true)
        {
            random.NextBytes(bytes);

            for (int i = 0; i < step; i++)
            {
                int alphabetIndex = bytes[i] & mask;

                if (alphabetIndex >= alphabet.Length) continue;
                idBuilder[cnt] = alphabet[alphabetIndex];
                if (++cnt == size)
                {
                    return new string(idBuilder);
                }
            }
        }
    }
}