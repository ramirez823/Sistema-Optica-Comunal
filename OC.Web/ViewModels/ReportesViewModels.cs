namespace OC.Web.ViewModels
{
    public class ReporteVentasViewModel
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? SucursalId { get; set; }

        public List<OC.Core.Domain.Entities.Sucursal> Sucursales { get; set; } = new();

        public decimal TotalGeneral { get; set; }
        public int TotalTransacciones { get; set; }
        public decimal PromedioVenta { get; set; }

        public List<string> EtiquetasFecha { get; set; } = new();
        public List<decimal> TotalesPorFecha { get; set; } = new();

        public List<string> EtiquetasSucursal { get; set; } = new();
        public List<decimal> TotalesPorSucursal { get; set; } = new();

        public List<string> EtiquetasMetodo { get; set; } = new();
        public List<int> ConteosPorMetodo { get; set; } = new();

        public List<ProductoTopItem> TopProductos { get; set; } = new();
    }

    public class ProductoTopItem
    {
        public string Nombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
        public decimal TotalGenerado { get; set; }
    }

    public class ReporteFidelizacionViewModel
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? SucursalId { get; set; }

        public List<OC.Core.Domain.Entities.Sucursal> Sucursales { get; set; } = new();

        public int TotalNuevos { get; set; }
        public int TotalRegulares { get; set; }
        public int TotalFrecuentes { get; set; }
        public int TotalEsporadicos { get; set; }

        public List<FidelizacionPacienteItem> Detalle { get; set; } = new();
    }

    public class FidelizacionPacienteItem
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public int VisitasEnPeriodo { get; set; }
        public string Clasificacion { get; set; } = string.Empty;
        public DateTime UltimaVisita { get; set; }
    }

    public class ReporteDemandaViewModel
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? SucursalId { get; set; }

        public List<OC.Core.Domain.Entities.Sucursal> Sucursales { get; set; } = new();

        public int TotalAgendadas { get; set; }
        public int TotalAtendidas { get; set; }
        public int TotalCanceladas { get; set; }
        public int TotalPendientes { get; set; }

        public List<string> EtiquetasMes { get; set; } = new();
        public List<int> AtendidaPorMes { get; set; } = new();
        public List<int> CanceladaPorMes { get; set; } = new();
        public List<int> PendientePorMes { get; set; } = new();
    }
}