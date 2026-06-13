using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class ValidarTotpViewModel
    {
        [Required(ErrorMessage = "El código TOTP es obligatorio")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe tener 6 dígitos")]
        [Display(Name = "Código de autenticador")]
        public string Codigo { get; set; } = string.Empty;
    }
}
