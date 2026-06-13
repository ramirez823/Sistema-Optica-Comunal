using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OC.Web.ViewModels
{
    public class EquipoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo es obligatorio")]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Marca { get; set; }

        [MaxLength(100)]
        public string? Modelo { get; set; }

        [MaxLength(100)]
        public string? Procesador { get; set; }

        [MaxLength(50)]
        public string? RAM { get; set; }

        [MaxLength(100)]
        public string? Disco { get; set; }

        [MaxLength(100)]
        public string? SistemaOperativo { get; set; }

        [MaxLength(50)]
        public string? VersionSO { get; set; }

        public int? UsuarioAsignadoId { get; set; }

        [MaxLength(50)]
        public string? NumeroSerie { get; set; }

        [MaxLength(50)]
        public string? Inventario { get; set; }

        public DateTime? FechaCompra { get; set; }

        [Range(0, 120)]
        public int? GarantiaMeses { get; set; }

        public string? Observaciones { get; set; }

        public bool Activo { get; set; } = true;

        // Para dropdown de usuarios
        public IEnumerable<SelectListItem>? UsuariosList { get; set; }
    }
}