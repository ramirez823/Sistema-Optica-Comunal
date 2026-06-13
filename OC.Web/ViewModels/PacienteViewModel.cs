using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class PacienteViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos son obligatorios")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = null!;

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [Display(Name = "Cédula")]
        [RegularExpression(@"^(\d-\d{4}-\d{4}|\d{9})$", ErrorMessage = "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789")]
        [StringLength(11, MinimumLength = 9, ErrorMessage = "Use el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789")]
        public string Cedula { get; set; } = null!;

        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [Display(Name = "Fecha de Nacimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; } = DateTime.Now.AddYears(-30);

        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Contraseña")]
        [DataType(DataType.Password)]
        public string? Contrasena { get; set; }

        [Display(Name = "Confirmar Contraseña")]
        [DataType(DataType.Password)]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmarContrasena { get; set; }
    }
}
