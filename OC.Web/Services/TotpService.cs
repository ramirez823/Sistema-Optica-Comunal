using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using System.Text;

namespace OC.Web.Services
{
    public class TotpService : ITotpService
    {
        private readonly IDataProtector _pacienteProtector;
        private readonly IDataProtector _staffProtector;

        public TotpService(IDataProtectionProvider dataProtectionProvider)
        {
            _pacienteProtector = dataProtectionProvider.CreateProtector("PacienteTotpSecret.v1");
            _staffProtector = dataProtectionProvider.CreateProtector("UsuarioTotpSecret.v1");
        }

        private IDataProtector GetProtector(bool forStaff) => forStaff ? _staffProtector : _pacienteProtector;

        public string GenerateSecretBase32()
        {
            var bytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(bytes);
        }

        public string ProtectSecret(string base32Secret, bool forStaff = false)
        {
            return GetProtector(forStaff).Protect(NormalizeBase32Secret(base32Secret));
        }

        public string UnprotectSecret(string protectedSecret, bool forStaff = false)
        {
            return GetProtector(forStaff).Unprotect(protectedSecret);
        }

        public bool VerifyCode(string base32Secret, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
            if (normalizedCode.Length != 6)
                return false;

            byte[] secretBytes;
            try
            {
                secretBytes = Base32Encoding.ToBytes(NormalizeBase32Secret(base32Secret));
            }
            catch
            {
                return false;
            }
            var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);

            return totp.VerifyTotp(
                normalizedCode,
                out _,
                new VerificationWindow(previous: 1, future: 1));
        }

        public string GetManualEntryKey(string base32Secret)
        {
            var chunks = Enumerable.Range(0, (base32Secret.Length + 3) / 4)
                .Select(i =>
                {
                    var start = i * 4;
                    var len = Math.Min(4, base32Secret.Length - start);
                    return base32Secret.Substring(start, len);
                });
            return string.Join(" ", chunks);
        }

        private static string NormalizeBase32Secret(string secret) =>
            new string(secret.Trim().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
    }
}
