using System;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    // Estados de cita
    public static class EstadoCita
    {
        public const string Pendiente = "Pendiente";
        public const string Confirmada = "Confirmada";
        public const string Cancelada = "Cancelada";
        public const string Atendida = "Atendida";
    }

    public class Cita
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public int SolicitudCitaId { get; set; }
        public int SucursalId { get; set; }
        public DateTime FechaHora { get; set; }
        public string? MotivoConsulta { get; set; }
        public string? ObservacionesEspecialista { get; set; }
        public string Estado { get; set; } = EstadoCita.Confirmada;
        public int? UsuarioAsignadoId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        /// <summary>Si el paciente desea recibir recordatorios y notificaciones para esta cita.</summary>
        public bool NotificacionesActivas { get; set; } = true;
        /// <summary>Canal de notificación: Email, SMS, WhatsApp.</summary>
        public string CanalNotificacion { get; set; } = "Email";

        public Paciente Paciente { get; set; } = null!;
        public SolicitudCita SolicitudCita { get; set; } = null!;
        public Sucursal Sucursal { get; set; } = null!;
        public Usuario? UsuarioAsignado { get; set; }
        public Expediente? Expediente { get; set; }
        public ICollection<EnvioNotificacion> EnviosNotificacion { get; set; } = new List<EnvioNotificacion>();
    }
}
