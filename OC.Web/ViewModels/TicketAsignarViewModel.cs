using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class TicketAsignarViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un técnico")]
        public int? TecnicoAsignadoId { get; set; }

        public IEnumerable<SelectListItem>? TecnicosList { get; set; }
    }
}