namespace Backend.Models.Domain;

public class Mailinglist
{
    public int Id { get; set; }

    public uint BitValue { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;
}