using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class SucursalViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre de la Sucursal")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = null!;

        // NUEVOS CAMPOS
        [Display(Name = "Teléfono adicional")]
        public string? TelefonoAdicional { get; set; }

        [Display(Name = "Horario de atención")]
        public string? HorarioAtencion { get; set; }

        [Display(Name = "Latitud")]
        public decimal? Latitud { get; set; }

        [Display(Name = "Longitud")]
        public decimal? Longitud { get; set; }
    }
}