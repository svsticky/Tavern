using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing activities and their related assets.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Retrieves a list of activities based on the specified criteria in the GetActivitiesDTO object. This method allows for dynamic filtering of activities based on various parameters such as date range, activity type, and other relevant attributes. The method returns an enumerable collection of ActivityResponseDTO objects that match the specified criteria, providing a structured representation of the activities for API responses. The retrieval process should be designed to be efficient and scalable, handling potential errors or exceptions that may arise during the data access while maintaining appropriate access control based on the user's role within the system.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the activities.</param>
    /// <param name="dto">The data transfer object containing the filtering criteria.</param>
    /// <returns>The list of activities matching the specified criteria.</returns>
    Task<IEnumerable<ActivityResponseDTO>> GetActivities(Guid userId, GetActivitiesDTO dto);

    /// <summary>
    /// Retrieves a specific activity by its ID. This method takes the user's ID and the activity's ID as parameters and returns an ActivityResponseDTO object that represents the details of the requested activity. The retrieval process should ensure that the user has appropriate access rights to view the activity details, and it should handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the activity.</param>
    /// <param name="id">The ID of the activity to retrieve.</param>
    /// <returns>The activity details, or null if not found.</returns>
    Task<ActivityResponseDTO?> GetActivity(Guid userId, uint id);

    /// <summary>
    /// Creates a new activity based on the provided PostActivityDTO object. This method takes the user's ID and the data transfer object containing the activity details as parameters, and it returns an Activity object that represents the newly created activity. The creation process should validate the input data, ensure that the user has appropriate permissions to create an activity, and handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system.
    /// </summary>
    /// <param name="userId">The ID of the user creating the activity.</param>
    /// <param name="dto">The data transfer object containing the activity details.</param>
    /// <returns>The newly created activity.</returns>
    Task<Activity> CreateActivity(Guid userId, PostActivityDTO dto);

    /// <summary>
    /// Deletes an existing activity based on the provided activity ID. This method takes the user's ID and the activity's ID as parameters and performs the deletion of the specified activity. The deletion process should ensure that the user has appropriate permissions to delete the activity, validate that the activity exists, and handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system.
    /// </summary>
    /// <param name="userId">The ID of the user deleting the activity.</param>
    /// <param name="id">The ID of the activity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteActivity(Guid userId, uint id);

    /// <summary>
    /// Applies a JSON Patch document to an existing activity based on the provided activity ID. This method takes the user's ID, the activity's ID, and the JSON Patch document as parameters, and it returns the updated Activity object after applying the patch. The patching process should ensure that the user has appropriate permissions to modify the activity, validate that the activity exists, and handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system. The method should also ensure that the JSON Patch document is correctly applied to the activity, allowing for partial updates of the activity's properties as specified in the patch document.
    /// </summary>
    /// <param name="userId">The ID of the user modifying the activity.</param>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="patchDoc">The JSON Patch document containing the updates.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PatchActivity(Guid userId, uint id, JsonPatchDocument<Activity> patchDoc, CancellationToken ct);
    
    /// <summary>
    /// Uploads a poster image for a specific activity based on the provided activity ID. This method takes the user's ID, the activity's ID, and the poster file as parameters, and it handles the process of uploading and associating the poster image with the specified activity. The upload process should ensure that the user has appropriate permissions to modify the activity, validate that the activity exists, and handle potential errors or exceptions that may arise during the file upload while maintaining data integrity and security within the system. The method should also ensure that the uploaded file is of an acceptable format and size for use as a poster image for the activity.
    /// </summary>
    /// <param name="userId">The ID of the user uploading the poster.</param>
    /// <param name="id">The ID of the activity for which to upload the poster.</param>
    /// <param name="poster">The poster file to upload.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UploadPoster(Guid userId, uint id, IFormFile? poster);

    /// <summary>
    /// Updates an existing activity based on the provided activity ID and the details specified in the PutActivityDTO object. This method takes the user's ID, the activity's ID, and the data transfer object containing the updated activity details as parameters, and it returns a task representing the asynchronous operation of updating the activity. The update process should ensure that the user has appropriate permissions to modify the activity, validate that the activity exists, and handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system. The method should also ensure that the updated details are correctly applied to the activity, allowing for a complete replacement of the activity's properties as specified in the PutActivityDTO object.
    /// </summary>
    /// <param name="userId">The ID of the user modifying the activity.</param>
    /// <param name="id">The ID of the activity to update.</param>
    /// <param name="dto">The data transfer object containing the updated activity details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateActivity(Guid userId, uint id, PutActivityDTO dto);

    /// <summary>
    /// Retrieves the poster image for a specific activity based on the provided activity ID. This method takes the user's ID, the activity's ID, and a boolean indicating whether to download the poster as parameters, and it returns a tuple containing the stream of the poster image, its content type, and an optional file name. The retrieval process should ensure that the user has appropriate permissions to view the activity's poster, validate that the activity exists, and handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system. The method should also ensure that the retrieved poster image is correctly formatted for display or download based on the specified parameters.
    /// </summary>
    /// <param name="userId">The ID of the user retrieving the poster.</param>
    /// <param name="id">The ID of the activity for which to retrieve the poster.</param>
    /// <param name="download">A boolean indicating whether to download the poster.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<(Stream Stream, string ContentType, string? FileName)?> GetPoster(Guid userId, uint id, bool download);

    /// <summary>
    /// Generates a CSV file containing the enrollment details for a specific activity based on the provided activity ID. This method takes the user's ID, the activity's ID, and a cancellation token as parameters, and it returns a tuple containing the byte array of the generated CSV file and its file name. The generation process should ensure that the user has appropriate permissions to access the enrollment details, validate that the activity exists, and handle potential errors or exceptions that may arise during the data access while maintaining data integrity and security within the system. The method should also ensure that the generated CSV file is correctly formatted to include relevant enrollment information such as participant names, contact details, and any other pertinent data associated with the enrollments for the specified activity.
    /// </summary>
    /// <param name="userId">The ID of the user generating the CSV.</param>
    /// <param name="activityId">The ID of the activity for which to generate enrollment details.</param>
    /// <param name="ct">The cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<(byte[] Content, string FileName)> GetEnrollmentsCsv(Guid userId, uint activityId, CancellationToken ct);
}
