namespace OC.Web.ViewModels
{
    public class VentaPorPacienteDto
    {
        public int Id { get; set; }
        public string NumeroFactura { get; set; } = string.Empty;
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
    }
}