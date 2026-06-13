using System.Text.RegularExpressions;

namespace OC.Web.Helpers
{
    /// <summary>
    /// Cédula en formato de visualización X-XXXX-XXXX (ej: 6-0424-0201).
    /// Se almacena en BD como 9 dígitos sin guiones.
    /// </summary>
    public static class CedulaValidation
    {
        /// <summary>Formato con guiones: 1 dígito, guión, 4 dígitos, guión, 4 dígitos.</summary>
        public const string FormatoRegex = @"^\d-\d{4}-\d{4}$";
        /// <summary>Formato solo dígitos (para BD): exactamente 9.</summary>
        public const string FormatoSoloDigitosRegex = @"^\d{9}$";
        public const int LongitudCedula = 9;
        public const string EjemploFormato = "1-2345-6789";

        /// <summary>Valida que la cédula tenga formato X-XXXX-XXXX (o 9 dígitos que se puedan formatear).</summary>
        public static bool EsFormatoValido(string? cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return false;
            var normalizada = Normalizar(cedula);
            if (normalizada.Length != LongitudCedula) return false;
            return Regex.IsMatch(normalizada, FormatoSoloDigitosRegex);
        }

        /// <summary>Valida formato con guiones X-XXXX-XXXX.</summary>
        public static bool EsFormatoConGuionesValido(string? cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return false;
            var t = cedula.Trim();
            return Regex.IsMatch(t, FormatoRegex);
        }

        /// <summary>Quita espacios y guiones, deja solo 9 dígitos (para guardar en BD y comparar).</summary>
        public static string Normalizar(string? cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula)) return string.Empty;
            return new string(cedula.Where(char.IsDigit).ToArray());
        }

        /// <summary>Formatea 9 dígitos a X-XXXX-XXXX para mostrar en UI.</summary>
        public static string FormatearParaMostrar(string? cedulaNormalizada)
        {
            if (string.IsNullOrWhiteSpace(cedulaNormalizada) || cedulaNormalizada.Length != LongitudCedula)
                return cedulaNormalizada ?? string.Empty;
            return $"{cedulaNormalizada[0]}-{cedulaNormalizada.Substring(1, 4)}-{cedulaNormalizada.Substring(5, 4)}";
        }
    }
}
