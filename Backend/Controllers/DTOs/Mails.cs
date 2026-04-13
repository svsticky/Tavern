using Backend.Models;

namespace Backend.Controllers.DTOs;

public class PostMailDTO
{
    public MailRecipient[]? Recipients { get; set; } = null!;
    public uint? ActivityId { get; set; }

    public string Subject { get; set; } = null!;

    public string HtmlContent { get; set; } = null!;
}