namespace Backend.Models;

/// <summary>
/// Controls where a curated mailing list is shown to members. General lists appear everywhere
/// (registration, account settings, the yearly renewal page); YearlyRenewalOnly lists appear only
/// on the yearly study-renewal page, alongside the General lists.
/// </summary>
public enum MailinglistVisibility
{
    /// <summary>Shown on registration, account settings, and the yearly renewal page.</summary>
    General,

    /// <summary>Shown only on the yearly renewal page, not on the everyday account settings page.</summary>
    YearlyRenewalOnly
}
