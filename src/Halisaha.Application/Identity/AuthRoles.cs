namespace Halisaha.Application.Identity;

public static class AuthRoles
{
    public const string Customer = "Customer";
    public const string BusinessOwner = "BusinessOwner";
    public const string BusinessStaff = "BusinessStaff";
    public const string SystemAdmin = "SystemAdmin";

    public static readonly string[] All = [Customer, BusinessOwner, BusinessStaff, SystemAdmin];
}

public static class AuthPolicies
{
    public const string BusinessAccess = "BusinessAccess";
}
