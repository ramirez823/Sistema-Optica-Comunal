using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Aro
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Precio { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool Activo { get; set; } = true;

        [MaxLength(512)]
        public string? RutaImagen { get; set; }
    }
}