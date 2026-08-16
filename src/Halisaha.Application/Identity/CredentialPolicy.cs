using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Halisaha.Application.Identity;

public static partial class CredentialPolicy
{
    public static IReadOnlyDictionary<string, string[]> ValidateRegistration(RegisterCommand command)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var displayName = command.DisplayName.Trim();
        var email = command.Email?.Trim();
        var phoneNumber = command.PhoneNumber?.Trim();

        if (displayName.Length is < 2 or > 100)
        {
            errors["displayName"] = ["Ad soyad 2 ile 100 karakter arasında olmalıdır."];
        }

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
        {
            errors["contact"] = ["E-posta veya telefon numarasından en az biri gereklidir."];
        }

        if (!string.IsNullOrWhiteSpace(email)
            && (!MailAddress.TryCreate(email, out var address)
                || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)))
        {
            errors["email"] = ["Geçerli bir e-posta adresi girin."];
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneNumberRegex().IsMatch(phoneNumber))
        {
            errors["phoneNumber"] = ["Telefon numarası +905551112233 biçiminde olmalıdır."];
        }

        var passwordErrors = ValidatePassword(command.Password);
        if (passwordErrors.Length > 0)
        {
            errors["password"] = passwordErrors;
        }

        return errors;
    }

    public static string[] ValidatePassword(string password)
    {
        var errors = new List<string>();
        if (password.Length < 10) errors.Add("Parola en az 10 karakter olmalıdır.");
        if (!password.Any(char.IsUpper)) errors.Add("Parola en az bir büyük harf içermelidir.");
        if (!password.Any(char.IsLower)) errors.Add("Parola en az bir küçük harf içermelidir.");
        if (!password.Any(char.IsDigit)) errors.Add("Parola en az bir rakam içermelidir.");
        return [.. errors];
    }

    [GeneratedRegex("^\\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneNumberRegex();
}
