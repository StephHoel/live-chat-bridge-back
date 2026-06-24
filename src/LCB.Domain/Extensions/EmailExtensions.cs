using System.Net.Mail;

namespace LCB.Domain.Extensions;

public static class EmailExtensions
{
    public static string NormalizeEmail(this string email)
    {
        return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
    }

    public static bool IsValidEmail(this string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}