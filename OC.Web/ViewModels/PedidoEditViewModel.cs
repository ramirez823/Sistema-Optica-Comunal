using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Domain.Entities;

namespace OC.Web.ViewModels
{
    public class PedidoEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un proveedor")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La fecha de entrega estimada es requerida")]
        [Display(Name = "Fecha de entrega estimada")]
        [DataType(DataType.Date)]
        public DateTime FechaEntregaEstimada { get; set; }

        [Display(Name = "Estado")]
        public EstadoPedido Estado { get; set; }

        public List<DetallePedidoViewModel> Detalles { get; set; } = new();

        public IEnumerable<SelectListItem>? Proveedores { get; set; }
        public IEnumerable<SelectListItem>? Productos { get; set; }
    }
}