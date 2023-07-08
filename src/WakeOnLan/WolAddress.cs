namespace WakeOnLan;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public readonly struct WolAddress :
#if NET8_0_OR_GREATER
    IUtf8SpanFormattable,
#endif
    ISpanParsable<WolAddress>, ISpanFormattable, IEquatable<WolAddress>
{
    private const char FormatCompact = 'M';
    private const char FormatDashed = 'D';
    private const char FormatColon = 'C';
    private const char FormatDotted = 'X';

    private readonly ulong _value;

    public WolAddressFamily AddressFamily { get; }

    public int Length => AddressFamily is WolAddressFamily.Eui48 ? 6 : 8;

    public ReadOnlySpan<byte> Address
    {
        get
        {
            ref var byteRef = ref Unsafe.As<ulong, byte>(ref Unsafe.AsRef(_value));
            return MemoryMarshal.CreateReadOnlySpan(ref byteRef, Length);
        }
    }

    public WolAddress(ReadOnlySpan<byte> data)
    {
        if (data.Length is not 6 and not 8)
        {
            throw new ArgumentException("MAC address must be either EUI-48 or EUI-64.", nameof(data));
        }

        Unsafe.SkipInit(out _value);

        ref var byteRef = ref Unsafe.As<ulong, byte>(ref Unsafe.AsRef(_value));
        data.CopyTo(MemoryMarshal.CreateSpan(ref byteRef, 8));
        AddressFamily = data.Length is 6 ? WolAddressFamily.Eui48 : WolAddressFamily.Eui64;
    }

    public static WolAddress Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException("Invalid MAC address.");
        }

        return result;
    }

    public static WolAddress Parse(string s, IFormatProvider? provider)
    {
        return Parse(s.AsSpan(), provider);
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out WolAddress result)
    {
        result = default;

        // Either EUI-48 or EUI-64 format
        // Allowed separators: <none>, :, -, .
        // Allowed lengths: 12 (no separator, EUI-48), 17 (separator, EUI-48), 16 (no separator, EUI-64), 23 (separator, EUI-64)
        if (s.Length is not 12 and not 17 and not 16 and not 23)
        {
            return false;
        }

        var isSeparatorUsed = s.Length is 17 or 23;
        var isEui48 = s.Length is 12 or 17;

        // Check destination length
        var bytesToWrite = isEui48 ? 6 : 8;

        Span<byte> destination = stackalloc byte[8]; // max EUI8
        var bytesWritten = 0;

        static byte ConvertHexDigit(char value) => (byte)(value - '0');

        static bool TryConvertHexToByteUppercase(char value, out byte result)
        {
            result = (byte)(value - 'A' + 10);
            return value is >= 'A' and <= 'F';
        }

        static bool TryConvertHexToByteLowercase(char value, out byte result)
        {
            result = (byte)(value - 'a' + 10);
            return value is >= 'a' and <= 'f';
        }

        static bool CheckSeparator(char separator, ref char separatorSlot)
        {
            const char NullSeparator = '\0';

            return separatorSlot is NullSeparator
                ? separator is ':' or '-' or '.'
                : separator == separatorSlot;
        }

        var separatorSlot = default(char);
        var letterConverterSlot = default(HexConverter?);

        static bool TryConvert(char value, ref HexConverter? letterConverterSlot, out byte result)
        {
            if (char.IsDigit(value))
            {
                result = ConvertHexDigit(value);
                return true;
            }

            letterConverterSlot ??= char.IsAsciiHexDigitUpper(value)
                ? TryConvertHexToByteUppercase
                : TryConvertHexToByteLowercase;

            return letterConverterSlot(value, out result);
        }

        // Validate characters
        for (var index = 0; index < s.Length;)
        {
            if ((isSeparatorUsed && index is not 0 && !CheckSeparator(s[index++], ref separatorSlot)) ||
                !TryConvert(s[index++], ref letterConverterSlot, out var hex1) ||
                !TryConvert(s[index++], ref letterConverterSlot, out var hex2))
            {
                return false;
            }

            destination[bytesWritten++] = (byte)((hex1 << 4) | hex2);
        }

        Debug.Assert(bytesWritten == bytesToWrite);

        result = new WolAddress(destination[..bytesWritten]);
        return true;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out WolAddress result)
    {
        return TryParse(s.AsSpan(), provider, out result);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        CheckFormat(format ?? string.Empty, out var formatStyle, out var length, out var uppercase);

        var state = (Uppercase: uppercase, Format: formatStyle, Address: this);

        return string.Create(length, state, static (span, state) =>
        {
            var (uppercase, format, address) = state;
            address.FormatInternal(span, out _, format, uppercase);
        });
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    private static byte GetSeparator(char format) => format switch
    {
        FormatDashed => (byte)'-',
        FormatColon => (byte)':',
        FormatDotted => (byte)'.',
        _ => default,
    };

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        CheckFormat(format, out var formatStyle, out var length, out var uppercase);

        bytesWritten = 0;

        if (utf8Destination.Length < length)
        {
            return false;
        }

        var separator = GetSeparator(formatStyle);
        var charOffset = uppercase ? 'A' : 'a';
        var address = Address;

        for (var index = 0; index < address.Length; index++)
        {
            if (index is not 0)
            {
                utf8Destination[bytesWritten++] = separator;
            }

            var value = address[index];

            // Convert to hex
            var hex1 = (byte)((value >> 4) & 0xF);
            var hex2 = (byte)(value & 0xF);

            utf8Destination[bytesWritten++] = (byte)(hex1 < 10 ? hex1 + '0' : hex1 - 10 + charOffset);
            utf8Destination[bytesWritten++] = (byte)(hex2 < 10 ? hex2 + '0' : hex2 - 10 + charOffset);
        }

        return true;
    }

    private void FormatInternal(Span<char> destination, out int charsWritten, char formatStyle, bool uppercase)
    {
        charsWritten = 0;

        var separator = GetSeparator(formatStyle);
        var charOffset = uppercase ? 'A' : 'a';
        var address = Address;

        for (var index = 0; index < address.Length; index++)
        {
            if (index is not 0 && formatStyle is not FormatCompact)
            {
                destination[charsWritten++] = (char)separator;
            }

            var value = address[index];
            var hex1 = (byte)((value >> 4) & 0xF);
            var hex2 = (byte)(value & 0xF);

            destination[charsWritten++] = (char)(hex1 < 10 ? hex1 + '0' : hex1 - 10 + charOffset);
            destination[charsWritten++] = (char)(hex2 < 10 ? hex2 + '0' : hex2 - 10 + charOffset);
        }
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        CheckFormat(format, out var formatStyle, out var length, out var uppercase);

        charsWritten = 0;

        if (destination.Length < length)
        {
            return false;
        }

        FormatInternal(destination, out charsWritten, formatStyle, uppercase);
        return true;
    }

    private void CheckFormat(ReadOnlySpan<char> format, out char formatStyle, out int length, out bool uppercase)
    {
        static Exception ThrowInvalidFormatString() => new FormatException("Format specifier was invalid.");

        var isEui48 = AddressFamily is WolAddressFamily.Eui48;

        if (format.IsEmpty)
        {
            formatStyle = FormatDashed;
            length = isEui48 ? 17 : 23;
            uppercase = true;
            return;
        }

        if (format.Length is not 1)
        {
            throw ThrowInvalidFormatString();
        }

        formatStyle = char.ToUpperInvariant(format[0]);
        uppercase = format[0] == formatStyle;

        if (formatStyle is not FormatCompact and not FormatDashed and not FormatColon and not FormatDotted)
        {
            throw ThrowInvalidFormatString();
        }

        if (formatStyle is FormatCompact)
        {
            length = isEui48 ? 12 : 16;
        }
        else
        {
            length = isEui48 ? 17 : 23;
        }
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is WolAddress address && address.Equals(this);
    }

    public bool Equals(WolAddress other)
    {
        return Address.SequenceEqual(other.Address);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(AddressFamily);
        hash.AddBytes(Address);

        return hash.ToHashCode();
    }

    public static bool operator ==(WolAddress left, WolAddress right) => left.Equals(right);

    public static bool operator !=(WolAddress left, WolAddress right) => !(left == right);
}

file delegate bool HexConverter(char c, out byte b);