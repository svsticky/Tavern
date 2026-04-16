using Backend.Models.Domain;

namespace Backend.Validators;

public static class AnswerValidator
{
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