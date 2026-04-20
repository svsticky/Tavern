namespace Backend.Validators;

public static class GroupValidator
{
    public static void ValidateName(string groupName)
    {
        if (groupName.Contains(';') || groupName.Contains(':'))
            throw new ArgumentException("Group names cannot contain ';' or ':'.");
    }
}
