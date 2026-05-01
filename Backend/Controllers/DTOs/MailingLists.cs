namespace Backend.Controllers.DTOs;

/// <summary>
/// Data Transfer Object for creating or updating a mailing list. The PostMailinglistDTO class encapsulates the necessary information required to create or update a mailing list entity, including the Name of the mailing list and the ServiceId that identifies the associated email service. This DTO is used in the Mailinglists controller to receive data from client requests when creating new mailing lists or updating existing ones, ensuring that the required fields are provided and structured correctly for processing by the underlying business logic in the MailinglistService.
/// </summary>
public class PostMailinglistDTO
{
    /// <inheritdoc cref="Domain.MailingList.Name"/>
    public string Name { get; set; } = null!;
    /// <inheritdoc cref="Domain.MailingList.ServiceId"/>
    public string ServiceId { get; set; } = null!;
}