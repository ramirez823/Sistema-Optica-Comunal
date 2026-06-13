using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class TicketClasificarViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar una prioridad")]
        public string Prioridad { get; set; } = string.Empty;

        public IEnumerable<SelectListItem>? TiposList { get; set; }
        public IEnumerable<SelectListItem>? PrioridadesList { get; set; }
    }
}