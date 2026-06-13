using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class RestablecerContrasenaViewModel
    {
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmarContrasena { get; set; } = null!;
    }
}
