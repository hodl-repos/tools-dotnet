using System;
using System.Runtime.CompilerServices;

namespace tools_dotnet.Utility
{
    public static class StringCaseExtensions
    {
        public static string? ToSnakeCase(this string? value) =>
            ConvertDelimitedCase(value, '_', upper: false);

        public static string? ToKebabCase(this string? value) =>
            ConvertDelimitedCase(value, '-', upper: false);

        public static string? ToDotCase(this string? value) =>
            ConvertDelimitedCase(value, '.', upper: false);

        public static string? ToCobolCase(this string? value) =>
            ConvertDelimitedCase(value, '-', upper: true);

        public static string? ToScreamingSnakeCase(this string? value) =>
            ConvertDelimitedCase(value, '_', upper: true);

        public static string? ToPascalCase(this string? value) =>
            ConvertPascalOrCamel(value, pascal: true);

        public static string? ToCamelCase(this string? value) =>
            ConvertPascalOrCamel(value, pascal: false);

        private static string? ConvertDelimitedCase(string? value, char separator, bool upper)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            int length = ComputeDelimitedLength(value.AsSpan());

            return string.Create(
                length,
                (value, separator, upper),
                static (dst, state) =>
                {
                    var (value, separator, upper) = state;
                    ReadOnlySpan<char> src = value.AsSpan();

                    int di = 0;
                    bool wroteAny = false;
                    CharType prevType = CharType.None;

                    for (int i = 0; i < src.Length; i++)
                    {
                        char c = src[i];
                        CharType currentType = GetCharType(c);

                        if (currentType == CharType.Separator)
                        {
                            prevType = CharType.Separator;
                            continue;
                        }

                        bool isWordStart = false;

                        if (!wroteAny)
                        {
                            isWordStart = true;
                        }
                        else if (prevType == CharType.Separator)
                        {
                            isWordStart = true;
                        }
                        else if (currentType == CharType.Upper)
                        {
                            if (prevType == CharType.Lower || prevType == CharType.Digit)
                            {
                                isWordStart = true;
                            }
                            else if (
                                (uint)(i + 1) < (uint)src.Length
                                && GetCharType(src[i + 1]) == CharType.Lower
                            )
                            {
                                isWordStart = true;
                            }
                        }
                        if (isWordStart && wroteAny)
                            dst[di++] = separator;

                        dst[di++] = upper
                            ? ToUpperAsciiInvariant(c, currentType)
                            : ToLowerAsciiInvariant(c, currentType);

                        wroteAny = true;
                        prevType = currentType;
                    }
                }
            );
        }

        private static string? ConvertPascalOrCamel(string? value, bool pascal)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            int length = ComputeJoinedLength(value.AsSpan());

            if (length == 0)
                return string.Empty;

            return string.Create(
                length,
                (value, pascal),
                static (dst, state) =>
                {
                    var (value, pascal) = state;
                    ReadOnlySpan<char> src = value.AsSpan();

                    int di = 0;
                    bool newWord = true;
                    bool firstWordWritten = false;
                    CharType prevType = CharType.None;

                    for (int i = 0; i < src.Length; i++)
                    {
                        char c = src[i];
                        CharType currentType = GetCharType(c);

                        if (currentType == CharType.Separator)
                        {
                            newWord = true;
                            prevType = CharType.Separator;
                            continue;
                        }

                        bool boundary = false;

                        if (di > 0 && prevType != CharType.Separator)
                        {
                            if (currentType == CharType.Upper)
                            {
                                if (prevType == CharType.Lower || prevType == CharType.Digit)
                                {
                                    boundary = true;
                                }
                                else if (
                                    (uint)(i + 1) < (uint)src.Length
                                    && GetCharType(src[i + 1]) == CharType.Lower
                                )
                                {
                                    boundary = true;
                                }
                            }
                        }

                        if (boundary)
                            newWord = true;

                        if (newWord)
                        {
                            bool makeUpper = pascal || firstWordWritten;
                            dst[di++] = makeUpper
                                ? ToUpperAsciiInvariant(c, currentType)
                                : ToLowerAsciiInvariant(c, currentType);

                            newWord = false;
                            firstWordWritten = true;
                        }
                        else
                        {
                            dst[di++] = ToLowerAsciiInvariant(c, currentType);
                        }

                        prevType = currentType;
                    }
                }
            );
        }

        private static int ComputeDelimitedLength(ReadOnlySpan<char> src)
        {
            int length = 0;
            bool wroteAny = false;
            CharType prevType = CharType.None;

            for (int i = 0; i < src.Length; i++)
            {
                CharType currentType = GetCharType(src[i]);

                if (currentType == CharType.Separator)
                {
                    prevType = CharType.Separator;
                    continue;
                }

                bool isWordStart = false;

                if (!wroteAny)
                {
                    isWordStart = true;
                }
                else if (prevType == CharType.Separator)
                {
                    isWordStart = true;
                }
                else if (currentType == CharType.Upper)
                {
                    if (prevType == CharType.Lower || prevType == CharType.Digit)
                    {
                        isWordStart = true;
                    }
                    else if (
                        (uint)(i + 1) < (uint)src.Length
                        && GetCharType(src[i + 1]) == CharType.Lower
                    )
                    {
                        isWordStart = true;
                    }
                }
                if (isWordStart && wroteAny)
                    length++;

                length++;
                wroteAny = true;
                prevType = currentType;
            }

            return length;
        }

        private static int ComputeJoinedLength(ReadOnlySpan<char> src)
        {
            int length = 0;

            for (int i = 0; i < src.Length; i++)
            {
                if (GetCharType(src[i]) != CharType.Separator)
                    length++;
            }

            return length;
        }

        private enum CharType : byte
        {
            None,
            Lower,
            Upper,
            Digit,
            Separator,
            Other,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CharType GetCharType(char c)
        {
            if ((uint)(c - 'a') <= ('z' - 'a'))
                return CharType.Lower;
            if ((uint)(c - 'A') <= ('Z' - 'A'))
                return CharType.Upper;
            if ((uint)(c - '0') <= 9)
                return CharType.Digit;
            if (c is '_' or '-' or '.' or ' ' or '/' or '\\')
                return CharType.Separator;
            return CharType.Other;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToLowerAsciiInvariant(char c, CharType type) =>
            type == CharType.Upper ? (char)(c | 0x20) : c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToUpperAsciiInvariant(char c, CharType type) =>
            type == CharType.Lower ? (char)(c & ~0x20) : c;
    }
}

