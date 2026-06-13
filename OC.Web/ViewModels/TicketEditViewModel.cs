using Microsoft.AspNetCore.Mvc.Rendering;

namespace OC.Web.ViewModels
{
    public class TicketEditViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;  // ← Agregar esta línea

        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public string? Tipo { get; set; }
        public int? TecnicoAsignadoId { get; set; }
        public int? EquipoId { get; set; }

        public IEnumerable<SelectListItem>? TecnicosList { get; set; }
        public IEnumerable<SelectListItem>? EstadosList { get; set; }
        public IEnumerable<SelectListItem>? TiposList { get; set; }
        public IEnumerable<SelectListItem>? EquiposList { get; set; }
        public IEnumerable<SelectListItem>? PrioridadesList { get; set; }
    }
}