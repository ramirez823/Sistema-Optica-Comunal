namespace OC.Web.ViewModels
{
    public class TicketCalificarViewModel
    {
        public int Id { get; set; }
        public string NumeroSeguimiento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? SolucionAplicada { get; set; }
        public DateTime? FechaResolucion { get; set; }
    }
}