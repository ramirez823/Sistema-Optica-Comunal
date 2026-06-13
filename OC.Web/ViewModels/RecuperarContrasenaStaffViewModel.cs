using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class RecuperarContrasenaStaffViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;
    }
}
