using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OC.Web.ViewModels
{
    public class PedidoCreateViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un proveedor")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de entrega estimada es requerida")]
        [Display(Name = "Fecha de entrega estimada")]
        [DataType(DataType.Date)]
        public DateTime FechaEntregaEstimada { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Productos")]
        public List<DetallePedidoViewModel> Detalles { get; set; } = new();

        // Para llenar dropdowns
        public IEnumerable<SelectListItem>? Proveedores { get; set; }
        public IEnumerable<SelectListItem>? Productos { get; set; }
    }
}