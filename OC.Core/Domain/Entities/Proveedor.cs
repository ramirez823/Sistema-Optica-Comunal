using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Proveedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número telefónico es obligatorio")]
        [StringLength(9, MinimumLength = 8, ErrorMessage = "El número debe tener 8 dígitos (con o sin guión)")]
        [RegularExpression(@"^\d{4}-?\d{4}$", ErrorMessage = "Formato inválido. Use 8 dígitos, con o sin guión (ej. 8888-1234)")]
        [Display(Name = "Teléfono")]
        [DataType(DataType.PhoneNumber)]
        public string NumeroTelefonico { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [MaxLength(255, ErrorMessage = "El correo no puede superar 255 caracteres")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo")]
        [DataType(DataType.EmailAddress)]
        public string Correo { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        // ========== CONTACTO ADICIONAL (opcional) ==========
        [MaxLength(100, ErrorMessage = "El nombre del contacto no puede superar 100 caracteres")]
        [Display(Name = "Contacto adicional (nombre)")]
        public string? ContactoAdicionalNombre { get; set; }

        [MaxLength(20, ErrorMessage = "El teléfono del contacto no puede superar 20 caracteres")]
        [RegularExpression(@"^[\d\s\-+()]*$", ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Contacto adicional (teléfono)")]
        [DataType(DataType.PhoneNumber)]
        public string? ContactoAdicionalTelefono { get; set; }
    }
}
