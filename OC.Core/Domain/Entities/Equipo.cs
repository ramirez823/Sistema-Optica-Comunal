using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Equipo
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Tipo { get; set; } = string.Empty; // Computadora, Laptop, Periférico, Monitor, etc.

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
        public Usuario? UsuarioAsignado { get; set; }

        [MaxLength(50)]
        public string? NumeroSerie { get; set; }

        [MaxLength(50)]
        public string? Inventario { get; set; }

        public DateTime? FechaCompra { get; set; }
        public int? GarantiaMeses { get; set; }

        public string? Observaciones { get; set; }

        public bool Activo { get; set; } = true;
    }
}