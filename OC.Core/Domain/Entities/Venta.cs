using OC.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.Core.Domain.Entities
{
    public class Venta
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;

        public int PacienteId { get; set; }
        public Paciente Paciente { get; set; } = null!;

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        public int SucursalId { get; set; }
        public Sucursal Sucursal { get; set; } = null!;


        // Nullable: solo se llena cuando la venta incluye lentes con receta
        public int? ValorClinicoId { get; set; }
        public ValorClinico? ValorClinico { get; set; }

        public MetodoPago MetodoPago { get; set; }
        public decimal Total { get; set; }
        public decimal Descuento { get; set; }
        public string? ReferenciaPago { get; set; }
        public string? RutaComprobante { get; set; }
        public string? Notas { get; set; }
        public DateTime FechaVenta { get; set; } = DateTime.Now;

        public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }
}