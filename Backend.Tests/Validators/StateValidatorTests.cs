using System.ComponentModel.DataAnnotations;
using Backend.Validators;

namespace Backend.Tests.Validators;

public class StateValidatorTests
{
    private class TestModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }

        [Range(1, 100, ErrorMessage = "Age must be between 1 and 100.")]
        public int Age { get; set; }
    }

    [Fact]
    public void Validate_NullObject_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => StateValidator.Validate<TestModel>(null!));
        Assert.Contains("Object to validate cannot be null", exception.Message);
    }

    [Fact]
    public void Validate_ValidObject_DoesNotThrow()
    {
        var model = new TestModel { Name = "John Doe", Age = 30 };
        
        var exception = Record.Exception(() => StateValidator.Validate(model));
        
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InvalidObject_ThrowsValidationExceptionWithErrors()
    {
        var model = new TestModel { Name = null, Age = 150 };
        
        var exception = Assert.Throws<ValidationException>(() => StateValidator.Validate(model));
        
        Assert.Contains("Name is required.", exception.Message);
        Assert.Contains("Age must be between 1 and 100.", exception.Message);
    }
}
