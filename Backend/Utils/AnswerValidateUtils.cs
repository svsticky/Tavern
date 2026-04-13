using Backend.Models.Domain;

namespace Backend.Utils;

public static class AnswerValidateUtils
{
    public static bool IsValidAnswer(string answer, QuestionType expectedType, string? options = null)
    {
        try
        {
            switch (expectedType)
            {
                case QuestionType.String:
                    return true;

                case QuestionType.Number:
                    return double.TryParse(answer, out _);

                case QuestionType.Boolean:
                    return bool.TryParse(answer, out _);

                case QuestionType.Date:
                    return DateTime.TryParse(answer, out _);

                case QuestionType.MultipleChoice:
                    string[] optionsArray = options?.Split(';').Select(o => o.Trim()).ToArray() ?? Array.Empty<string>();

                    if (options == null || options.Length == 0)
                        throw new ArgumentException("Options must be provided for multiple choice questions.");

                    return options.Contains(answer);

                case QuestionType.DateTime:
                    return DateTime.TryParse(answer, out _);

                default:
                    throw new ArgumentException("Unsupported question type.");
            }
        }
        catch
        {
            return false;
        }
    }
}