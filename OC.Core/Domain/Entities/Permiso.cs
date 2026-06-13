using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OC.Core.Domain.Entities
{
    public class Permiso
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public string Tipo { get; set; } = string.Empty; // Vacaciones, SinPago, Incapacidad, Otro
        public string? Motivo { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public int? AprobadoPorId { get; set; }
        public Usuario? AprobadoPor { get; set; }

        /// <summary>Ruta relativa (wwwroot) del documento de incapacidad, si aplica.</summary>
        public string? RutaDocumentoIncapacidad { get; set; }
    }
}
