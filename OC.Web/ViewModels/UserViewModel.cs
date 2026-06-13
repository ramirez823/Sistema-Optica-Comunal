using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; } // Agregamos el ID para saber a quién editamos


        // Campos de entrada
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre Completo")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // QUITAMOS [Required] de aquí. Lo validaremos en el Controller.
        [DataType(DataType.Password)]
        public string? Password { get; set; } // Hacemos nullable

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "Debe seleccionar un Rol")]
        public int RoleId { get; set; }

        [Display(Name = "Sucursal")]
        [Required(ErrorMessage = "Debe seleccionar una Sucursal")]
        public int SucursalId { get; set; }

        // ===== NUEVOS CAMPOS =====
        [Required(ErrorMessage = "La cédula es obligatoria")]
        [MaxLength(20)]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;

        [Display(Name = "Salario Base (₡)")]
        [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a 0")]
        public decimal? SalarioBase { get; set; }

        [Display(Name = "Fecha de Contratación")]
        [DataType(DataType.Date)]
        public DateTime? FechaContratacion { get; set; }

        [Display(Name = "Banco")]
        public string? Banco { get; set; }

        [Display(Name = "IBAN (20 dígitos)")]
        public string? IbanBloque1 { get; set; }
        public string? IbanBloque2 { get; set; }
        public string? IbanBloque3 { get; set; }
        public string? IbanBloque4 { get; set; }
        public string? IbanBloque5 { get; set; }

        public string? NumeroCuentaIBAN { get; set; }
        // =========================

        public IEnumerable<SelectListItem>? BancosList { get; set; }
        public IEnumerable<SelectListItem>? RolesList { get; set; }
        public IEnumerable<SelectListItem>? SucursalesList { get; set; }
    }
}