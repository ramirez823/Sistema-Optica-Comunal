using System;

namespace OC.Core.Domain.Entities
{
    public class SolicitudCita
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;
        public string? Motivo { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobada, Rechazada
        public DateTime? FechaAprobacion { get; set; }
        public int? UsuarioAprobadorId { get; set; } // Recepcionista que aprueba

        // Relaciones
        public Paciente Paciente { get; set; } = null!;
        public Usuario? UsuarioAprobador { get; set; }
    }
}
