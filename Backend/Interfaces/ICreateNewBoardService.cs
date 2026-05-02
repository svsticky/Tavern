namespace Backend.Interfaces;

/// <summary>
/// Service interface for promoting candidate board members to actual board members at the start of a new financial year.
/// </summary>
public interface ICreateNewBoardService
{
    /// <summary>
    /// Promotes candidate board members to actual board members for the new financial year. This method should be called at the start of each financial year to ensure that the board is updated with the new members. It checks if the promotion has already been done for the current year to avoid duplicate promotions. If not, it retrieves the candidate board members from the previous year and creates new board memberships for them in the current year, while also enqueuing messages to update their group memberships in Keycloak.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PromoteCandidateBoardToBoardAsync();
}