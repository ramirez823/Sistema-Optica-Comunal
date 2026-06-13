using System;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    /// <summary>Estados de la orden de trabajo (lentes). OT-HU-023: al pasar a Lista se notifica al paciente.</summary>
    public static class EstadoOrdenTrabajo
    {
        public const string Pendiente = "Pendiente";
        public const string EnProceso = "EnProceso";
        public const string Lista = "Lista";
        public const string Entregada = "Entregada";
    }

    /// <summary>Orden de trabajo para fabricación/entrega de lentes. Vinculada al paciente para notificación cuando está lista.</summary>
    public class OrdenTrabajo
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public int SucursalId { get; set; }
        /// <summary>Opcional: venta asociada (lentes vendidos en esa factura).</summary>
        public int? VentaId { get; set; }
        public string Estado { get; set; } = EstadoOrdenTrabajo.Pendiente;
        public string? Referencia { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        /// <summary>Cuándo pasó a estado Lista (para notificación).</summary>
        public DateTime? FechaLista { get; set; }

        // Datos de fabricación del lente
        public decimal? PD { get; set; }
        public string? TipoLente { get; set; }
        public string? MaterialLente { get; set; }
        public string? Tratamientos { get; set; }
        public string? LaboratorioExterno { get; set; }

        public Paciente Paciente { get; set; } = null!;
        public Sucursal Sucursal { get; set; } = null!;
        public Venta? Venta { get; set; }
        public ICollection<EnvioNotificacion> EnviosNotificacion { get; set; } = new List<EnvioNotificacion>();
    }
}
