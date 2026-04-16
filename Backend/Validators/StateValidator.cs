using System.ComponentModel.DataAnnotations;

namespace Backend.Interfaces;

public static class StateValidator
{
    public static void Validate<T>(T obj)
    {
        if(obj == null)
        {
            throw new ArgumentNullException(nameof(obj), "Object to validate cannot be null.");
        }

        var validationContext = new ValidationContext(obj);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            obj,
            validationContext,
            validationResults,
            validateAllProperties: true
        );

        if (!isValid)
        {
            throw new ValidationException(
                string.Join(", ", validationResults.Select(r => r.ErrorMessage))
            );
        }
    }
}