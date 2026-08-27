using Backend.Utils;
using Xunit;

namespace Backend.Tests.Utils;

public class CsvUtilsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Escape_NullOrEmpty_ReturnsEmptyString(string? value)
    {
        Assert.Equal("", CsvUtils.Escape(value));
    }

    [Fact]
    public void Escape_PlainValue_ReturnsUnchanged()
    {
        Assert.Equal("Lidmaatschap", CsvUtils.Escape("Lidmaatschap"));
    }

    [Theory]
    [InlineData("=cmd|'/c calc'!A1")]
    [InlineData("+1234")]
    [InlineData("-1234")]
    [InlineData("@SUM(A1:A2)")]
    public void Escape_FormulaTrigger_IsPrefixedWithSingleQuote(string value)
    {
        var result = CsvUtils.Escape(value);
        Assert.StartsWith("'" + value[0], result);
    }

    [Fact]
    public void Escape_ContainsDelimiter_IsWrappedInQuotes()
    {
        Assert.Equal("\"a;b\"", CsvUtils.Escape("a;b"));
    }

    [Fact]
    public void Escape_ContainsComma_IsWrappedInQuotes()
    {
        Assert.Equal("\"a,b\"", CsvUtils.Escape("a,b"));
    }

    [Fact]
    public void Escape_ContainsNewline_IsWrappedInQuotes()
    {
        var result = CsvUtils.Escape("a\nb");
        Assert.Equal("\"a\nb\"", result);
    }

    [Fact]
    public void Escape_ContainsDoubleQuote_IsDoubledAndWrapped()
    {
        Assert.Equal("\"say \"\"hi\"\"\"", CsvUtils.Escape("say \"hi\""));
    }

    [Fact]
    public void FormatLine_NullField_RendersAsEmpty()
    {
        Assert.Equal(";b", CsvUtils.FormatLine(null, "b"));
    }

    [Fact]
    public void FormatLine_JoinsFieldsWithSemicolon()
    {
        Assert.Equal("a;b;c", CsvUtils.FormatLine("a", "b", "c"));
    }

    [Fact]
    public void FormatLine_Decimal_UsesInvariantDecimalSeparator()
    {
        var result = CsvUtils.FormatLine(1234.5m);
        Assert.Equal("1234.50", result);
    }

    [Fact]
    public void FormatLine_Double_UsesInvariantDecimalSeparator()
    {
        var result = CsvUtils.FormatLine(1234.5d);
        Assert.Equal("1234.50", result);
    }

    [Fact]
    public void FormatLine_FieldNeedingEscaping_IsEscaped()
    {
        var result = CsvUtils.FormatLine("a;b", "plain");
        Assert.Equal("\"a;b\";plain", result);
    }
}
