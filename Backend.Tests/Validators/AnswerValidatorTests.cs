using Backend.Models.Domain;
using Backend.Validators;

namespace Backend.Tests.Validators;

public class AnswerValidatorTests
{
    [Fact]
    public void IsValidAnswer_String_AlwaysPasses()
    {
        var exception = Record.Exception(() => AnswerValidator.IsValidAnswer("any text", QuestionType.String));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12.34")]
    [InlineData("-456")]
    public void IsValidAnswer_Number_ValidValues_Passes(string value)
    {
        var exception = Record.Exception(() => AnswerValidator.IsValidAnswer(value, QuestionType.Number));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34.56")]
    [InlineData("")]
    public void IsValidAnswer_Number_InvalidValues_Throws(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer(value, QuestionType.Number));
        Assert.Contains("Invalid answer for question type Number", exception.Message);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("false")]
    [InlineData("False")]
    public void IsValidAnswer_Boolean_ValidValues_Passes(string value)
    {
        var exception = Record.Exception(() => AnswerValidator.IsValidAnswer(value, QuestionType.Boolean));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("")]
    public void IsValidAnswer_Boolean_InvalidValues_Throws(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer(value, QuestionType.Boolean));
        Assert.Contains("Invalid answer for question type Boolean", exception.Message);
    }

    [Theory]
    [InlineData("2026-05-30")]
    [InlineData("05/30/2026")]
    public void IsValidAnswer_Date_ValidValues_Passes(string value)
    {
        var exception = Record.Exception(() => AnswerValidator.IsValidAnswer(value, QuestionType.Date));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("")]
    public void IsValidAnswer_Date_InvalidValues_Throws(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer(value, QuestionType.Date));
        Assert.Contains("Invalid answer for question type Date", exception.Message);
    }

    [Theory]
    [InlineData("2026-05-30 13:00:00")]
    [InlineData("2026-05-30T13:00:00Z")]
    public void IsValidAnswer_DateTime_ValidValues_Passes(string value)
    {
        var exception = Record.Exception(() => AnswerValidator.IsValidAnswer(value, QuestionType.DateTime));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("not-a-datetime")]
    [InlineData("")]
    public void IsValidAnswer_DateTime_InvalidValues_Throws(string value)
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer(value, QuestionType.DateTime));
        Assert.Contains("Invalid answer for question type DateTime", exception.Message);
    }

    [Theory]
    [InlineData("Red", "Red;Blue;Green")]
    [InlineData("Blue", "Red; Blue; Green")] // Handles trimming
    public void IsValidAnswer_MultipleChoice_ValidOption_Passes(string value, string options)
    {
        var exception = Record.Exception(() => AnswerValidator.IsValidAnswer(value, QuestionType.MultipleChoice, options));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Yellow", "Red;Blue;Green")]
    [InlineData("Red", "Blue;Green")]
    public void IsValidAnswer_MultipleChoice_InvalidOption_Throws(string value, string options)
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer(value, QuestionType.MultipleChoice, options));
        Assert.Contains("Invalid answer for question type MultipleChoice", exception.Message);
    }

    [Fact]
    public void IsValidAnswer_MultipleChoice_NullOptions_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer("Red", QuestionType.MultipleChoice, null));
        Assert.Contains("Invalid answer for question type MultipleChoice", exception.Message);
    }

    [Fact]
    public void IsValidAnswer_MultipleChoice_EmptyOptions_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer("Red", QuestionType.MultipleChoice, ""));
        Assert.Contains("Invalid answer for question type MultipleChoice", exception.Message);
    }

    [Fact]
    public void IsValidAnswer_InvalidQuestionType_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => AnswerValidator.IsValidAnswer("Any", (QuestionType)99));
        Assert.Contains("Invalid answer for question type", exception.Message);
    }
}
