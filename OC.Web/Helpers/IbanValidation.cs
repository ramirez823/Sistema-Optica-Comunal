using System.Text.RegularExpressions;

namespace OC.Web.Helpers
{
    /// <summary>IBAN Costa Rica: CR + 20 dígitos (22 caracteres).</summary>
    public static class IbanValidation
    {
        public const string Prefijo = "CR";
        public const int LongitudTotal = 22;
        public const int LongitudDigitos = 20;

        public static string SoloDigitos20(string? bloque1, string? bloque2, string? bloque3, string? bloque4, string? bloque5)
        {
            var raw = string.Concat(
                SoloDigitos(bloque1),
                SoloDigitos(bloque2),
                SoloDigitos(bloque3),
                SoloDigitos(bloque4),
                SoloDigitos(bloque5));
            return raw.Length > LongitudDigitos ? raw[..LongitudDigitos] : raw;
        }

        public static string ConstruirIban(string? bloque1, string? bloque2, string? bloque3, string? bloque4, string? bloque5)
        {
            return Prefijo + SoloDigitos20(bloque1, bloque2, bloque3, bloque4, bloque5);
        }

        public static bool EsIbanValido(string? iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            var t = iban.Trim().ToUpperInvariant();
            return Regex.IsMatch(t, @"^CR\d{20}$");
        }

        public static void DescomponerParaFormulario(string? ibanGuardado, out string b1, out string b2, out string b3, out string b4, out string b5)
        {
            b1 = b2 = b3 = b4 = b5 = string.Empty;
            if (string.IsNullOrWhiteSpace(ibanGuardado)) return;

            var digits = SoloDigitos(ibanGuardado);
            if (digits.StartsWith("CR", StringComparison.OrdinalIgnoreCase))
                digits = digits[2..];
            digits = digits.Length > LongitudDigitos ? digits[..LongitudDigitos] : digits.PadRight(LongitudDigitos, '0');

            if (digits.Length < LongitudDigitos) return;

            b1 = digits.Substring(0, 4);
            b2 = digits.Substring(4, 4);
            b3 = digits.Substring(8, 4);
            b4 = digits.Substring(12, 4);
            b5 = digits.Substring(16, 4);
        }

        private static string SoloDigitos(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return new string(value.Where(char.IsDigit).ToArray());
        }
    }
}
