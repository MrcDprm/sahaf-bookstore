namespace Sahaf.Utility;

// Which status changes are allowed. Kept free of the database so the rules
// can be unit tested and are the same in the controller and the views.
//
//   Pending ──paid──▶ Approved ──▶ Processing ──▶ Shipped
//      │                 │              │
//      └──── Cancelled ◀─┴──────────────┘   (refunded if it was paid)
public static class OrderWorkflow
{
    public static bool CanStartProcessing(string orderStatus) => orderStatus == SD.StatusApproved;

    public static bool CanShip(string orderStatus) =>
        orderStatus is SD.StatusApproved or SD.StatusInProcess;

    public static bool CanCancel(string orderStatus) =>
        orderStatus is SD.StatusPending or SD.StatusApproved or SD.StatusInProcess;

    // A cancelled order that was already paid by card is refunded through Stripe.
    public static bool NeedsRefund(string paymentStatus) => paymentStatus == SD.PaymentStatusApproved;

    // Company orders are shipped first and paid later (net terms).
    public static bool CanPayLater(string orderStatus, string paymentStatus) =>
        paymentStatus == SD.PaymentStatusDelayedPayment && orderStatus != SD.StatusCancelled;

    // Steps shown on the order timeline; a cancelled order has no position.
    public static readonly string[] Steps = [SD.StatusPending, SD.StatusApproved, SD.StatusInProcess, SD.StatusShipped];

    public static int StepIndex(string orderStatus) => Array.IndexOf(Steps, orderStatus);
}
