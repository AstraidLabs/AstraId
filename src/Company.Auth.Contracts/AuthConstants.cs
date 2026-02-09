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

    public static class Permissions
    {
        public const string SystemAdmin = "system.admin";

        public static class Governance
        {
            public const string UserLifecycleManage = "governance.user-lifecycle.manage";
            public const string InactivityPolicyManage = "governance.inactivity.manage";
        }

        public static class Gdpr
        {
            public const string Read = "gdpr.read";
            public const string Export = "gdpr.export";
            public const string Erase = "gdpr.erase";
            public const string RetentionManage = "gdpr.retention.manage";
        }
    }
}
