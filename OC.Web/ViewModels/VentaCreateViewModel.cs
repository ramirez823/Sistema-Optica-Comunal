using OC.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class VentaCreateViewModel
    {
        [Required(ErrorMessage = "Seleccione un paciente")]
        public int PacienteId { get; set; }

        public int? ValorClinicoId { get; set; }

        [Required(ErrorMessage = "Seleccione un método de pago")]
        public MetodoPago MetodoPago { get; set; }

        public decimal Descuento { get; set; }

        public string? ReferenciaPago { get; set; }

        public string? Notas { get; set; }

        // El carrito viaja como JSON serializado desde el cliente
        [Required(ErrorMessage = "Debe agregar al menos un producto al carrito")]
        public string DetallesJson { get; set; } = string.Empty;
    }

    // Modelo interno para deserializar el JSON del carrito
    public class DetalleVentaInputModel
    {
        public int? ProductoId { get; set; }
        public string DescripcionSnapshot { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}