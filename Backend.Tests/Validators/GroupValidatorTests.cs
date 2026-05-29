using Backend.Validators;

namespace Backend.Tests.Validators;

public class GroupValidatorTests
{
    [Theory]
    [InlineData("ValidName")]
    [InlineData("Valid-Name")]
    [InlineData("Valid_Name")]
    public void ValidateName_WithValidName_DoesNotThrow(string name)
    {
        var exception = Record.Exception(() => GroupValidator.ValidateName(name));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Invalid;Name")]
    [InlineData("Invalid:Name")]
    [InlineData(";Invalid")]
    [InlineData("Invalid:")]
    public void ValidateName_WithReservedDelimiters_ThrowsArgumentException(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => GroupValidator.ValidateName(name));
        Assert.Equal("Group names cannot contain ';' or ':'.", exception.Message);
    }
}
