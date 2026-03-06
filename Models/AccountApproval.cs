namespace Library_Management_system.Models;

public static class AccountApproval
{
    public const string ClaimType = "AccountApprovalStatus";
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Canceled = "canceled";

    public static string Normalize(string? value)
    {
        if (string.Equals(value, Pending, StringComparison.OrdinalIgnoreCase))
        {
            return Pending;
        }

        if (string.Equals(value, Canceled, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            return Canceled;
        }

        return Approved;
    }

    public static string ToLabel(string value)
    {
        return Normalize(value) switch
        {
            Pending => "Pending",
            Canceled => "Canceled",
            _ => "Approved"
        };
    }
}
