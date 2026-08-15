namespace Backend.Controllers.DTOs;

/// <summary>
/// Represents the DTO for a file result, containing the necessary information to represent a file response, including the file stream and its content type. The FileResultDto is used to transfer file data from the server to the client when returning a file as part of a response, allowing for the proper handling and representation of the file data based on its content type in the client application.
/// </summary>
public class FileResultDto
{
    /// <summary>
    /// The stream representing the file data to be returned in the response. This field is required to provide the actual file content that will be sent to the client, allowing for the proper handling and representation of the file data based on its content type in the client application.
    /// </summary>
    public required Stream Stream { get; set; }

    /// <summary>
    /// The content type of the file being returned in the response. This field is required to specify the MIME type of the file, allowing for the proper handling and representation of the file data based on its content type in the client application. The content type helps the client application determine how to process and display the file data appropriately based on its format and type.
    /// </summary>
    public required string ContentType { get; set; }
}
