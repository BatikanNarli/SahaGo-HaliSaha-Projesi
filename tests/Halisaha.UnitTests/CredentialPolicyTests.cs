using Halisaha.Application.Identity;

namespace Halisaha.UnitTests;

public sealed class CredentialPolicyTests
{
    [Fact]
    public void RegistrationRequiresEmailOrInternationalPhone()
    {
        var errors = CredentialPolicy.ValidateRegistration(
            new RegisterCommand("Oyuncu", null, null, "Guvenli1234"));

        Assert.Contains("contact", errors.Keys);
    }

    [Fact]
    public void PasswordRequiresLengthUpperLowerAndDigit()
    {
        var errors = CredentialPolicy.ValidatePassword("zayif");

        Assert.Equal(3, errors.Length);
    }

    [Fact]
    public void ValidRegistrationHasNoPolicyErrors()
    {
        var errors = CredentialPolicy.ValidateRegistration(
            new RegisterCommand("Oyuncu", "oyuncu@example.test", "+905551112233", "Guvenli1234"));

        Assert.Empty(errors);
    }
}
