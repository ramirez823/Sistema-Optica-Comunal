using OC.Core.Domain.Entities;

namespace OC.Web.ViewModels
{
    public class FacturaViewModel
    {
        public Venta Venta { get; set; } = null!;
        public ValorClinico? ValorClinico { get; set; }
    }
}