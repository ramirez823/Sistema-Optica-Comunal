using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class DocumentoExpediente
    {
        public int Id { get; set; }

        [Required]
        public int ExpedienteId { get; set; }
        public Expediente Expediente { get; set; }

        [Required]
        public string NombreArchivo { get; set; }

        [Required]
        public string RutaArchivo { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.Now;
    }
}