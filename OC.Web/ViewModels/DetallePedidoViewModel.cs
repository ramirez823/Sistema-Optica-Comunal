using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class DetallePedidoViewModel
    {
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        // Para mostrar en la vista
        public string? NombreProducto { get; set; }
        public decimal CostoUnitario { get; set; }
    }
}