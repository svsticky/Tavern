using Microsoft.AspNetCore.Http;
using NSubstitute;
using Backend.Validators;

namespace Backend.Tests.Validators;

public class ExtensionValidatorTests
{
    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("IMAGE.JPEG", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.gif", "image/gif")]
    public void ValidatePosterExtension_AllowedExtensionsAndMimeTypes_DoesNotThrow(string fileName, string contentType)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);

        var exception = Record.Exception(() => ExtensionValidator.ValidatePosterExtension(file));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("image.pdf", "application/pdf")]
    [InlineData("image.exe", "image/jpeg")]
    [InlineData("image", "image/jpeg")]
    public void ValidatePosterExtension_DisallowedExtensionsOrMimeTypes_ThrowsArgumentException(string fileName, string contentType)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);

        var exception = Assert.Throws<ArgumentException>(() => ExtensionValidator.ValidatePosterExtension(file));

        Assert.Equal("Invalid file extension.", exception.Message);
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("IMAGE.PNG", "image/png")]
    public void ValidateProfilePictureExtension_AllowedExtensionsAndMimeTypes_DoesNotThrow(string fileName, string contentType)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);

        var exception = Record.Exception(() => ExtensionValidator.ValidateProfilePictureExtension(file));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("image.gif", "image/gif")] // Gif is allowed for posters but not profile pictures
    [InlineData("image.pdf", "application/pdf")]
    public void ValidateProfilePictureExtension_DisallowedExtensionsOrMimeTypes_ThrowsArgumentException(string fileName, string contentType)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(fileName);
        file.ContentType.Returns(contentType);

        var exception = Assert.Throws<ArgumentException>(() => ExtensionValidator.ValidateProfilePictureExtension(file));

        Assert.Equal("Invalid profile picture extension.", exception.Message);
    }
}
