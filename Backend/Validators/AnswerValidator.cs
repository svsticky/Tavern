using Backend.Models.Domain;

namespace Backend.Validators;

/// <summary>
/// Provides validation helpers for checking whether specification answers match a question type.
/// </summary>
public static class AnswerValidator
{
    /// <summary>
    /// Validates whether a raw answer value is compatible with the expected question type.
    /// </summary>
    /// <param name="answer">The submitted answer value.</param>
    /// <param name="expectedType">The expected question type.</param>
    /// <param name="options">The allowed options for multiple-choice questions.</param>
    /// <exception cref="ArgumentException">Thrown when the answer does not match the expected type.</exception>
    public static void IsValidAnswer(string answer, QuestionType expectedType, string? options = null)
    {
        try
        {
            switch (expectedType)
            {
                case QuestionType.String:
                    break;

                case QuestionType.Number:
                    if (!double.TryParse(answer, out _))
                        throw new ArgumentException("Answer must be a valid number.");
                    break;

                case QuestionType.Boolean:
                    if (!bool.TryParse(answer, out _))
                        throw new ArgumentException("Answer must be a valid boolean.");
                    break;

                case QuestionType.Date:
                    if (!DateTime.TryParse(answer, out _))
                        throw new ArgumentException("Answer must be a valid date.");
                    break;

                case QuestionType.MultipleChoice:
                    string[] optionsArray = options?.Split(';').Select(o => o.Trim()).ToArray() ?? Array.Empty<string>();

                    if (options == null || options.Length == 0)
                        throw new ArgumentException("Options must be provided for multiple choice questions.");

                    if (!options.Contains(answer))
                        throw new ArgumentException("Answer must be one of the provided options.");
                    break;

                case QuestionType.DateTime:
                    if (!DateTime.TryParse(answer, out _))
                        throw new ArgumentException("Answer must be a valid date and time.");
                    break;

                default:
                    throw new ArgumentException("Unsupported question type.");
            }
        }
        catch
        {
            throw new ArgumentException($"Invalid answer for question type {expectedType}.");
        }
    }
}
