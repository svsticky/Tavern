namespace Backend.Controllers.DTOs;

/// <summary>
/// Represents the response returned after successfully uploading a profile picture. The UploadPictureResponse class encapsulates the essential information about the uploaded image, primarily the generated path where the profile picture is stored. This DTO is used to communicate the result of the upload operation back to the client, allowing it to reference the new profile picture in subsequent requests or updates. By providing a clear and concise response structure, this class facilitates seamless integration between the client and server during profile picture management operations.
/// </summary>
public class UploadPictureResponse
{
    /// <summary>
    /// Gets or sets the generated path of the uploaded profile picture. This path is typically a relative or encoded reference to the location where the image is stored, which can be used for retrieval or display purposes in the client application.
    /// </summary>
    public required string? Path { get; set; }
}
