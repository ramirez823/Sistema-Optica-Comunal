using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OC.Core.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty; // En producción esto será un Hash
        public bool Activo { get; set; } = true;

        // (Foreign Keys)
        public int RolId { get; set; }
        public Rol Rol { get; set; } = null!;

        public int SucursalId { get; set; }
        public Sucursal Sucursal { get; set; } = null!;

        // ===== NUEVOS CAMPOS PARA RH =====

        [Required, MaxLength(20)]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;  


        [Display(Name = "Salario Base (₡)")]
        [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a 0")]
        public decimal? SalarioBase { get; set; }  // Nullable porque algunos usuarios (ej. pacientes) no aplican

        [Display(Name = "Fecha de Contratación")]
        [DataType(DataType.Date)]
        public DateTime? FechaContratacion { get; set; }

        [Display(Name = "Banco")]
        [MaxLength(100)]
        public string? Banco { get; set; }

        [Display(Name = "Número de Cuenta IBAN")]
        [MaxLength(22)]
        public string? NumeroCuentaIBAN { get; set; }

        /// <summary>Trabajador nuevo: debe cambiar la contraseña temporal en el primer acceso.</summary>
        public bool DebeCambiarContrasena { get; set; }

        public string? TotpSecretProtegido { get; set; }
        public bool TotpHabilitado { get; set; }
        public DateTime? TotpConfiguradoEnUtc { get; set; }

        public string? TokenRecuperacion { get; set; }
        public DateTime? FechaExpiracionToken { get; set; }
        // ==================================
    }
}