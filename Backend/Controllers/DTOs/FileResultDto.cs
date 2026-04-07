namespace Backend.Controllers.DTOs;

public class FileResultDto
{
    public required Stream Stream { get; set; }
    public required string ContentType { get; set; }
}