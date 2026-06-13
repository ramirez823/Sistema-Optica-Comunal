using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El SKU es requerido")]
        [MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0")]
        public decimal CostoUnitario { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0")]
        public int Stock { get; set; }

        public bool Activo { get; set; } = true;

        /// <summary>Ruta relativa bajo wwwroot (ej. /uploads/productos/...).</summary>
        public string? RutaImagen { get; set; }

        public string? DescripcionCorta { get; set; }
        public bool Destacado { get; set; }
        public string? Categoria { get; set; }
        public decimal PrecioPublico { get; set; }
    }
}
