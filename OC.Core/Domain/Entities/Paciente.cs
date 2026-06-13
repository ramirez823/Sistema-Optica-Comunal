using System;
using System.Collections.Generic;

namespace OC.Core.Domain.Entities
{
    public class Paciente
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string Contrasena { get; set; } = string.Empty; 
        public DateTime FechaNacimiento { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public string? TokenRecuperacion { get; set; }
        public DateTime? FechaExpiracionToken { get; set; }
        public string? TotpSecretProtegido { get; set; }
        public bool TotpHabilitado { get; set; } = false;
        public DateTime? TotpConfiguradoEnUtc { get; set; }

        // WEB-HU-028: bloqueo temporal por intentos fallidos
        public int IntentosFallidosLogin { get; set; } = 0;
        public DateTime? BloqueadoHastaUtc { get; set; }
        public bool BloqueadoPermanentemente { get; set; } = false;

        // Propiedad calculada (útil para la UI)
        public string NombreCompleto => $"{Nombres} {Apellidos}";

        // Relaciones
        public ICollection<SolicitudCita> SolicitudesCitas { get; set; } = new List<SolicitudCita>();
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    }
}