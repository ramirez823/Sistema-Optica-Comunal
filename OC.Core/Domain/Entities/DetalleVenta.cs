using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OC.Core.Domain.Entities
{
    public class DetalleVenta
    {
        public int Id { get; set; }

        public int VentaId { get; set; }
        public Venta Venta { get; set; } = null!;

        // Nullable: productos del inventario lo tienen, lentes con precio libre no
        public int? ProductoId { get; set; }
        public Producto? Producto { get; set; }

        public string DescripcionSnapshot { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}