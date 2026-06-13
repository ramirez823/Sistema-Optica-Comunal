using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class DetallePedido
    {
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "El costo unitario debe ser mayor o igual a 0")]
        public decimal CostoUnitario { get; set; } // Costo al momento del pedido
    }
}