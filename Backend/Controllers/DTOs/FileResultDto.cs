namespace Backend.Controllers.DTOs;

public class FileResultDto
{
    public Stream Stream { get; set; } = default!;
    public string ContentType { get; set; } = default!;
}