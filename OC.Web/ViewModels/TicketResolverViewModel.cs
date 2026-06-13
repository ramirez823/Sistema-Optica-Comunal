using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class TicketResolverViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaAsignacion { get; set; }

        [Required(ErrorMessage = "Debe indicar la solución aplicada")]
        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string SolucionAplicada { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Máximo 500 caracteres")]
        public string? ObservacionesInternas { get; set; }
    }
}