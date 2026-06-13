namespace OC.Web.Helpers
{
    public static class AuthSessionKeys
    {
        public const string StaffPendingUserId = "Auth:StaffPendingUserId";
        public const string StaffPendingRememberMe = "Auth:StaffPendingRememberMe";
        public const string StaffTotpSetupSecret = "Auth:StaffTotpSetupSecret";

        public const string PacientePendingId = "Auth:PacientePendingId";
        public const string PacientePendingRememberMe = "Auth:PacientePendingRememberMe";

        public const string AdminExemptEmail = "admin@optica.com";

        public static bool EsCorreoExentoTotp(string? correo) =>
            !string.IsNullOrWhiteSpace(correo) &&
            correo.Trim().Equals(AdminExemptEmail, StringComparison.OrdinalIgnoreCase);
    }
}
