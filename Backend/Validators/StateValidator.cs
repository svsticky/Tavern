using System.ComponentModel.DataAnnotations;

namespace Backend.Validators;

/// <summary>
/// Provides object-state validation based on data-annotation attributes.
/// </summary>
public static class StateValidator
{
    /// <summary>
    /// Validates an object against its data-annotation rules and throws when invalid.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    /// <param name="obj">The object to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the object is <c>null</c>.</exception>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
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
