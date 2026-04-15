using Backend.Models;

namespace Backend.Controllers.DTOs;

public abstract class AbstractPostMailDTO
{
    public required string Subject { get; set; }

    public required string HtmlContent { get; set; }
}

public class PostMailDTO : AbstractPostMailDTO
{
    public required MailRecipient[] Recipients { get; set; }
}

public class PostActivityMailDTO : AbstractPostMailDTO
{
    public required uint ActivityId { get; set; }
    public bool IncludeWaitingList { get; set; }
}