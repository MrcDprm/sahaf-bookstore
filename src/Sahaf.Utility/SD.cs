namespace Sahaf.Utility;

// Static details: role names and order/payment statuses used across layers.
public static class SD
{
    public const string Role_Customer = "Customer";
    public const string Role_Company = "Company";
    public const string Role_Admin = "Admin";
    public const string Role_Employee = "Employee";

    public static readonly string[] Roles = [Role_Customer, Role_Company, Role_Admin, Role_Employee];

    public const string StatusPending = "Pending";
    public const string StatusApproved = "Approved";
    public const string StatusInProcess = "Processing";
    public const string StatusShipped = "Shipped";
    public const string StatusCancelled = "Cancelled";
    public const string StatusRefunded = "Refunded";

    public const string PaymentStatusPending = "Pending";
    public const string PaymentStatusApproved = "Approved";
    public const string PaymentStatusDelayedPayment = "ApprovedForDelayedPayment";
    public const string PaymentStatusRejected = "Rejected";

    // Company accounts pay by invoice within this many days.
    public const int CompanyPaymentTermDays = 30;
}
