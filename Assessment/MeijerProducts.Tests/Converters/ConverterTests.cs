using System.Globalization;
using MeijerProducts.Converters;
using MeijerProducts.Models;
using Xunit;

namespace MeijerProducts.Tests.Converters;

public class NullToBoolConverterTests
{
    private static object? Convert(object? value) =>
        new NullToBoolConverter().Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

    [Fact]
    public void Convert_WhenValueIsNull_ReturnsFalse()
    {
        Assert.Equal(false, Convert(null));
    }

    [Fact]
    public void Convert_WhenValueIsNotNull_ReturnsTrue()
    {
        Assert.Equal(true, Convert(new ProductDetail()));
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupported()
    {
        var converter = new NullToBoolConverter();

        Assert.Throws<NotSupportedException>(
            () => converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture));
    }
}

public class StringToBoolConverterTests
{
    private static object? Convert(object? value) =>
        new StringToBoolConverter().Convert(value, typeof(bool), null, CultureInfo.InvariantCulture);

    [Fact]
    public void Convert_WhenStringIsNull_ReturnsFalse()
    {
        Assert.Equal(false, Convert(null));
    }

    [Fact]
    public void Convert_WhenStringIsEmpty_ReturnsFalse()
    {
        // Drives the error-label IsVisible binding on both pages - an empty ErrorMessage
        // must keep the label hidden.
        Assert.Equal(false, Convert(string.Empty));
    }

    [Fact]
    public void Convert_WhenStringHasContent_ReturnsTrue()
    {
        Assert.Equal(true, Convert("Product not found."));
    }

    [Fact]
    public void Convert_WhenValueIsNotAString_ReturnsFalse()
    {
        Assert.Equal(false, Convert(42));
    }

    [Fact]
    public void ConvertBack_Always_ThrowsNotSupported()
    {
        var converter = new StringToBoolConverter();

        Assert.Throws<NotSupportedException>(
            () => converter.ConvertBack(true, typeof(object), null, CultureInfo.InvariantCulture));
    }
}
