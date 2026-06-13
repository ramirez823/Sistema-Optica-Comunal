using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OC.Web.ViewModels
{
    public class TicketCierreViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? TiempoResolucion { get; set; }

        [Required(ErrorMessage = "Debe ingresar observaciones de cierre")]
        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string Observaciones { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string? SolucionAplicada { get; set; }

        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string? TiempoDedicado { get; set; }

        [Range(1, 5, ErrorMessage = "Seleccione una calificación entre 1 y 5")]
        public int? SatisfaccionUsuario { get; set; }

        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? ComentariosAdicionales { get; set; }

        public bool RequiereSeguimiento { get; set; }

        // Listas para selects
        public IEnumerable<SelectListItem>? SatisfaccionList { get; set; }
    }
}