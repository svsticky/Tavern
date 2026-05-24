namespace Backend.Models.Domain;

/// <summary>
/// Represents a payment made by a member for either an enrollment or a membership. A payment has properties such as price, payment intent URL, and timestamps for when it was paid. This entity is used to manage and track payments within the system, allowing for better financial management and integration with payment processing services like Mollie. The Payment class serves as a base class for specific types of payments, such as MembershipPayment and EnrollmentPayment, which can have additional properties related to their specific contexts.
/// </summary>
public abstract class Payment 
{
    /// <summary>
    /// The unique identifier of the payment, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The price of the payment.
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// The identifier provided by the Payment Service for this payment. This is used to track the payment in the Payment Service system and to correlate it with the corresponding payment record in our system. This property is required for all payments, as it is essential for processing and verifying payments through the Payment Service payment gateway.
    /// </summary>
    public required string PaymentServiceId { get; set; }

    /// <summary>
    /// The URL provided by the Payment Service for the payment intent. This URL is used to redirect the member to the Payment Service payment page where they can complete the payment process. This property is required for all payments, as it is essential for facilitating the payment process through the Payment Service payment gateway and ensuring that members can easily access the payment page to complete their transactions.
    /// </summary>
    public required string PaymentIntentUrl { get; set; }

    /// <summary>
    /// The timestamp indicating when the payment was paid. This is used to track when the payment was completed and can be useful for financial reporting, auditing, and determining the status of the payment. This property is nullable because a payment may be created before it is completed, and in such cases, the PaidAt timestamp would not be set until the payment is successfully processed.
    /// </summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>
    /// The identifier of the member who made the payment. This is a foreign key referencing the Member entity. This property is nullable because even if the member is removed from the database, we may want to keep the payment record for historical and auditing purposes. In such cases, the MemberId would be set to null to indicate that the member associated with the payment has been deleted, while still retaining the payment information for reference.
    /// </summary>
    public Guid? MemberId { get; set; }
   
    /// <summary>
    /// The member who made the payment. This is a navigation property that allows access to the related Member entity. This property is nullable because even if the member is removed from the database, we may want to keep the payment record for historical and auditing purposes. In such cases, the Member navigation property would be set to null to indicate that the member associated with the payment has been deleted, while still retaining the payment information for reference.
    /// </summary>
    public Member? Member { get; set; }

    /// <summary>
    /// The identifier of the corresponding entry in the accounting tool, if applicable. This is used to correlate the payment record in our system with the corresponding financial record in the accounting tool. This property is nullable because not all payments may have a corresponding entry in the accounting tool, especially if they are manually marked as paid or if there are issues with the payment processing that prevent it from being recorded in the accounting tool. In such cases, the AccountingToolEntryId would be set to null to indicate that there is no corresponding entry for this payment in the accounting tool.
    /// </summary>
    public Guid? AccountingToolEntryId { get; set; }

    /// <summary>
    /// Indicates whether the payment was manually marked as paid by an administrator. This is used to differentiate between payments that were processed through the normal payment flow and those that were manually marked as paid, which may require different handling in terms of accounting and reporting. This property is set to false by default, and can be set to true by an administrator when they manually mark a payment as paid, allowing for better tracking and management of payments that may not have gone through the standard payment processing flow.
    /// </summary>
    public bool ManuallyMarkedAsPaid { get; set; } = false;
}

/// <summary>
/// Represents a payment made for a membership. This class inherits from the Payment base class and does not add any additional properties, as all relevant information for a membership payment is already captured in the base Payment class. This entity is used to specifically represent payments that are made for memberships within the system, allowing for better organization and management of different types of payments while still utilizing the common properties defined in the Payment base class.
/// </summary>
public class MembershipPayment : Payment
{

}

/// <summary>
/// Represents a payment made for an enrollment. This class inherits from the Payment base class and includes additional properties specific to enrollment payments, such as the associated activity. This entity is used to specifically represent payments that are made for enrollments within the system, allowing for better organization and management of different types of payments while still utilizing the common properties defined in the Payment base class. The ActivityId property is a foreign key referencing the Activity entity, and the Activity navigation property allows access to the related Activity entity, providing context for the enrollment payment and enabling better tracking and reporting of payments related to specific activities.
/// </summary>
public class EnrollmentPayment : Payment
{
    /// <summary>
    /// The identifier of the activity associated with this enrollment payment. This is a foreign key referencing the Activity entity. This property is nullable because in some cases, an enrollment payment may not be directly associated with a specific activity, such as when a payment is made for a general enrollment or when the activity information is not available at the time of payment. In such cases, the ActivityId would be set to null to indicate that there is no specific activity associated with this enrollment payment.
    /// </summary>
    public uint? ActivityId { get; set; }

    /// <summary>
    /// The activity associated with this enrollment payment. This is a navigation property that allows access to the related Activity entity. This property is nullable because in some cases, an enrollment payment may not be directly associated with a specific activity, such as when a payment is made for a general enrollment or when the activity information is not available at the time of payment. In such cases, the Activity navigation property would be set to null to indicate that there is no specific activity associated with this enrollment payment, while still retaining the payment information for reference.
    /// </summary>
    public Activity? Activity { get; set; } = null!;
}

/// <summary>
/// Represents the balance of an enrollment, which includes the enrollment itself and the corresponding balance amount. This entity is used to manage and track the financial balance associated with a specific enrollment, allowing for better financial management and reporting related to enrollments within the system. The Enrollment property is required to ensure that each EnrollmentBalance is associated with a specific enrollment, while the Balance property captures the current financial balance for that enrollment, which can be used for various purposes such as determining outstanding payments or providing financial summaries for members and administrators.
/// </summary>
public class EnrollmentBalance
{
    /// <summary>
    /// The enrollment associated with this balance. This is a required property that references the specific enrollment for which the balance is being tracked. The Enrollment property allows access to the related Enrollment entity, providing context for the balance and enabling better tracking and reporting of financial information related to specific enrollments within the system.
    /// </summary>
    public required Enrollment Enrollment { get; set; }

    /// <summary>
    /// The current financial balance for the associated enrollment. This property captures the amount that is currently owed or has been paid for the enrollment, allowing for better financial management and reporting related to enrollments within the system. The Balance property is required to ensure that each EnrollmentBalance has a defined financial balance, which can be used for various purposes such as determining outstanding payments, providing financial summaries for members and administrators, and facilitating financial decision-making related to enrollments.
    /// </summary>
    public required decimal Balance { get; set; }
}

/// <summary>
/// Represents a payment made for a Payment Service fee. This class inherits from the Payment base class and does not add any additional properties, as all relevant information for a Payment Service fee payment is already captured in the base Payment class. This entity is used to specifically represent payments that are made for Payment Service fees within the system, allowing for better organization and management of different types of payments while still utilizing the common properties defined in the Payment base class. Payment Service fee payments are typically associated with processing fees charged by the Payment Service payment gateway for handling transactions, and this entity allows for better tracking and reporting of these specific types of payments within the system.
/// </summary>
public class PaymentServiceFeePayment : Payment
{
    
}