using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class ExpedienteCreateViewModel
    {
        public int CitaId { get; set; }

        [Required(ErrorMessage = "El motivo de la consulta es obligatorio.")]
        [Display(Name = "Motivo de la consulta")]
        [MaxLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres.")]
        public string MotivoConsulta { get; set; } = string.Empty;

        [Display(Name = "Observaciones adicionales")]
        [MaxLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
        public string? Observaciones { get; set; }

        // Datos de la cita para mostrar
        public string? NombrePaciente { get; set; }
        public DateTime FechaCita { get; set; }
        public int PacienteId { get; set; }

        public bool CitaAtendida { get; set; }
    }
}