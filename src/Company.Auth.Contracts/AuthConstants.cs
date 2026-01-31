namespace Company.Auth.Contracts;

public static class AuthConstants
{
    public const string DefaultIssuer = "https://localhost:7001/";

    public static class Scopes
    {
        public const string OpenId = "openid";
        public const string Profile = "profile";
        public const string Email = "email";
        public const string OfflineAccess = "offline_access";

        public const string ApiScopePrefix = "api.";

        public static string ApiScope(string name) => $"{ApiScopePrefix}{name}";
        public static string ApiScopeRead(string name) => $"{ApiScopePrefix}{name}.read";
        public static string ApiScopeWrite(string name) => $"{ApiScopePrefix}{name}.write";
        public static string ApiScopeAdmin(string name) => $"{ApiScopePrefix}{name}.admin";
    }

    public static class ClaimTypes
    {
        public const string Permission = "permission";
        public const string Tenant = "tenant";
    }
}
