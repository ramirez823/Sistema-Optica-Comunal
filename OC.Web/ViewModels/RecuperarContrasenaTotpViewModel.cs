using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class RecuperarContrasenaTotpViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cédula es obligatoria")]
        [Display(Name = "Cédula")]
        public string Cedula { get; set; } = string.Empty;
    }
}
