using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string NumeroSeguimiento { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Asignado, En Proceso, Resuelto, Cerrado

        [MaxLength(20)]
        public string? Prioridad { get; set; } // Baja, Media, Alta, Urgente

        [MaxLength(30)]
        public string? Tipo { get; set; } // Hardware, Software, Red, Periférico, Otro

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaAsignacion { get; set; }
        public DateTime? FechaCierre { get; set; }

        // SLA
        public DateTime? FechaRespuestaEsperada { get; set; }
        public DateTime? FechaResolucionEsperada { get; set; }
        public DateTime? FechaPrimeraRespuesta { get; set; }
        public bool SLA_CumplidoRespuesta { get; set; }
        public bool SLA_CumplidoResolucion { get; set; }
        public string? SLA_Observacion { get; set; }
        public DateTime? FechaUltimaAlertaSLA { get; set; }

        // Nuevos campos para resolución y calificación
        public DateTime? FechaResolucion { get; set; }           // Cuando el técnico marca como resuelto
        public string? SolucionAplicada { get; set; }            // Solución que verá el cliente
        public string? ObservacionesInternas { get; set; }       // Notas internas del técnico
        public string? TiempoDedicado { get; set; }              // Calculado automáticamente al resolver

        // Calificación del cliente
        public int? CalificacionCliente { get; set; }            // 1-5
        public string? ComentarioCliente { get; set; }
        public DateTime? FechaCalificacion { get; set; }

        // Reapertura
        public bool Reabierto { get; set; }
        public string? MotivoReapertura { get; set; }
        public int? ReabiertoPorId { get; set; }
        public DateTime? FechaReapertura { get; set; }

        // Relaciones
        public int CreadoPorId { get; set; }
        public Usuario CreadoPor { get; set; } = null!;

        public int? EquipoId { get; set; }
        public Equipo? Equipo { get; set; }

        public int? TecnicoAsignadoId { get; set; }
        public Usuario? TecnicoAsignado { get; set; }

        // Campo legacy (opcional, se mantiene por compatibilidad)
        public string? ObservacionesCierre { get; set; }
    }
}