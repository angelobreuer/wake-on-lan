namespace WakeOnLan.Tests;

using System.Text;

public class WolAddressTests
{
    [Theory]
    [InlineData("01-23-45-67-89-AB")]
    [InlineData("01-23-45-67-89-ab")]
    [InlineData("01.23.45.67.89.AB")]
    [InlineData("01.23.45.67.89.ab")]
    [InlineData("01:23:45:67:89:AB")]
    [InlineData("01:23:45:67:89:ab")]
    [InlineData("0123456789AB")]
    [InlineData("0123456789ab")]
    public void TestParseEui48(string addressValue)
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };

        // Act
        var address = WolAddress.Parse(addressValue, provider: null);

        // Assert
        Assert.Equal(addressBytes, address.Address.ToArray());
    }

    [Theory]
    [InlineData("01-23-45-67-89-AB-CD-EF")]
    [InlineData("01-23-45-67-89-ab-cd-ef")]
    [InlineData("01.23.45.67.89.AB.CD.EF")]
    [InlineData("01.23.45.67.89.ab.cd.ef")]
    [InlineData("01:23:45:67:89:AB:CD:EF")]
    [InlineData("01:23:45:67:89:ab:cd:ef")]
    [InlineData("0123456789ABCDEF")]
    [InlineData("0123456789abcdef")]
    public void TestParseEui64(string addressValue)
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };

        // Act
        var address = WolAddress.Parse(addressValue, provider: null);

        // Assert
        Assert.Equal(addressBytes, address.Address.ToArray());
    }

    [Theory]
    [InlineData("01-23-45-67-89-AB", "")]
    [InlineData("01-23-45-67-89-AB", "D")]
    [InlineData("01-23-45-67-89-ab", "d")]
    [InlineData("01.23.45.67.89.AB", "X")]
    [InlineData("01.23.45.67.89.ab", "x")]
    [InlineData("01:23:45:67:89:AB", "C")]
    [InlineData("01:23:45:67:89:ab", "c")]
    [InlineData("0123456789AB", "M")]
    [InlineData("0123456789ab", "m")]
    public void TestFormatEui48(string addressValue, string format)
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.ToString(format, formatProvider: null);

        // Assert
        Assert.Equal(addressValue, result);
    }

    [Theory]
    [InlineData("01-23-45-67-89-AB-CD-EF", "")]
    [InlineData("01-23-45-67-89-AB-CD-EF", "D")]
    [InlineData("01-23-45-67-89-ab-cd-ef", "d")]
    [InlineData("01.23.45.67.89.AB.CD.EF", "X")]
    [InlineData("01.23.45.67.89.ab.cd.ef", "x")]
    [InlineData("01:23:45:67:89:AB:CD:EF", "C")]
    [InlineData("01:23:45:67:89:ab:cd:ef", "c")]
    [InlineData("0123456789ABCDEF", "M")]
    [InlineData("0123456789abcdef", "m")]
    public void TestFormatEui64(string addressValue, string format)
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.ToString(format, formatProvider: null);

        // Assert
        Assert.Equal(addressValue, result);
    }

    [Fact]
    public void TestFamilyEui48()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.AddressFamily;

        // Assert
        Assert.Equal(WolAddressFamily.Eui48, result);
    }

    [Fact]
    public void TestFamilyEui64()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.AddressFamily;

        // Assert
        Assert.Equal(WolAddressFamily.Eui64, result);
    }

    [Fact]
    public void TestLengthEui48()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.Length;

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void TestLengthEui64()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.Length;

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void TestEqualsEui48()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var result = address1.Equals(address2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TestEqualsEui64()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var result = address1.Equals(address2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TestEqualsEui48Object()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var result = address1.Equals((object)address2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TestEqualsEui64Object()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var result = address1.Equals((object)address2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TestEqualsEui48ObjectNull()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address1 = new WolAddress(addressBytes1);

        // Act
        var result = address1.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TestEqualsEui64ObjectNull()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address1 = new WolAddress(addressBytes1);

        // Act
        var result = address1.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TestEqualsEui48ObjectOtherType()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address1 = new WolAddress(addressBytes1);

        // Act
        var result = address1.Equals(123);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TestEqualsEui64ObjectOtherType()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, };
        var address1 = new WolAddress(addressBytes1);

        // Act
        var result = address1.Equals(123);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TestGetHashCodeEqualsIfSame()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var hashCode1 = address1.GetHashCode();
        var hashCode2 = address2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void TestGetHashCodeNotEqualsIfDifferent()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x00, 0x00 };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x00, 0x01 };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var hashCode1 = address1.GetHashCode();
        var hashCode2 = address2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void TestEqualOperator()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x00, 0x00 };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x00, 0x00 };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var result = address1 == address2;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TestNotEqualOperator()
    {
        // Arrange
        var addressBytes1 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x00, 0x00 };
        var addressBytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x00, 0x01 };
        var address1 = new WolAddress(addressBytes1);
        var address2 = new WolAddress(addressBytes2);

        // Act
        var result = address1 != address2;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TestToStringEui48()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.ToString();

        // Assert
        Assert.Equal("01-23-45-67-89-AB", result);
    }

    [Fact]
    public void TestToStringEui64()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        var address = new WolAddress(addressBytes);

        // Act
        var result = address.ToString();

        // Assert
        Assert.Equal("01-23-45-67-89-AB-CD-EF", result);
    }

    [Fact]
    public void TestToStringThrowsFormatExceptionIfFormatTooLong()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);

        // Act
        var exception = Assert.Throws<FormatException>(() => address.ToString("XX", null));

        // Assert
        Assert.Equal("Format specifier was invalid.", exception.Message);
    }

    [Fact]
    public void TestToStringThrowsFormatExceptionIfFormatInvalid()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);

        // Act
        var exception = Assert.Throws<FormatException>(() => address.ToString("Y", null));

        // Assert
        Assert.Equal("Format specifier was invalid.", exception.Message);
    }

    [Fact]
    public void TestTryFormatFalseIfDestinationTooShort()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);
        var destination = new char[4];

        // Act
        var result = address.TryFormat(destination, out _, "", null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TestTryFormatUtf8FalseIfDestinationTooShort()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);
        var destination = new byte[4];

        // Act
        var result = address.TryFormat(destination, out _, "", null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TestTryFormatUtf8Uppercase()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);
        var destination = new byte[17];

        // Act
        var result = address.TryFormat(destination, out var charsWritten, "D", null);

        // Assert
        Assert.True(result);
        Assert.Equal(17, charsWritten);
        Assert.Equal("01-23-45-67-89-AB", Encoding.ASCII.GetString(destination));
    }

    [Fact]
    public void TestTryFormatUtf8Lowercase()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);
        var destination = new byte[17];

        // Act
        var result = address.TryFormat(destination, out var charsWritten, "d", null);

        // Assert
        Assert.True(result);
        Assert.Equal(17, charsWritten);
        Assert.Equal("01-23-45-67-89-ab", Encoding.ASCII.GetString(destination));
    }

    [Fact]
    public void TestTryFormatUppercase()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);
        var destination = new char[17];

        // Act
        var result = address.TryFormat(destination, out var charsWritten, "D", null);

        // Assert
        Assert.True(result);
        Assert.Equal(17, charsWritten);
        Assert.Equal("01-23-45-67-89-AB", destination);
    }

    [Fact]
    public void TestTryFormatLowercase()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, };
        var address = new WolAddress(addressBytes);
        var destination = new char[17];

        // Act
        var result = address.TryFormat(destination, out var charsWritten, "d", null);

        // Assert
        Assert.True(result);
        Assert.Equal(17, charsWritten);
        Assert.Equal("01-23-45-67-89-ab", destination);
    }

    [Fact]
    public void TestConstructorThrowsIfNot6Or8BytesLong()
    {
        // Arrange
        var addressBytes = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, };
        var exception = Assert.Throws<ArgumentException>(() => new WolAddress(addressBytes));

        // Assert
        Assert.Equal("MAC address must be either EUI-48 or EUI-64. (Parameter 'data')", exception.Message);
    }

    [Theory]
    [InlineData("01-23-45-67-89-Ab")]
    [InlineData("01-23-45-67-89-AB-CD-Ef")]
    [InlineData("01-23-45-67-89-AB-CD")]
    [InlineData("01-23-45-67-89-AB.CD")]
    [InlineData("01-23-45-67-89-AB:CD")]
    [InlineData("01-23-45-67-89-AB@CD")]
    [InlineData("0@-23-45-67-89-AB-CD")]
    [InlineData("00@23@45@67@89@AB@CD")]
    public void TestTryParseFalseIfInvalidMacAddress(string value)
    {
        // Arrange
        var result = WolAddress.TryParse(value, null, out _);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("01-23-45-67-89-Ab")]
    [InlineData("01-23-45-67-89-AB-CD-Ef")]
    [InlineData("01-23-45-67-89-AB-CD")]
    [InlineData("01-23-45-67-89-AB.CD")]
    [InlineData("01-23-45-67-89-AB:CD")]
    [InlineData("01-23-45-67-89-AB@CD")]
    [InlineData("0@-23-45-67-89-AB-CD")]
    [InlineData("00@23@45@67@89@AB@CD")]
    public void TestParseThrowsIfMacAddressInvalid(string value)
    {
        // Arrange
        var exception = Assert.Throws<FormatException>(() => WolAddress.Parse(value, null));

        // Assert
        Assert.Equal("Invalid MAC address.", exception.Message);
    }
}