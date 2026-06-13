using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Expediente
    {
        public int Id { get; set; }

        [Required]
        public int CitaId { get; set; }
        public Cita Cita { get; set; }

        [Required]
        public string MotivoConsulta { get; set; }

        public string Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public ICollection<ValorClinico> ValoresClinicos { get; set; }
        public ICollection<DocumentoExpediente> Documentos { get; set; }
    }
}