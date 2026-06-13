using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class SolicitudCitaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un paciente")]
        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Display(Name = "Motivo de la Cita")]
        [MaxLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string? Motivo { get; set; }

        [Display(Name = "Fecha de Solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Pendiente";

        // Para mostrar información del paciente
        public string? NombrePaciente { get; set; }
        public string? CedulaPaciente { get; set; }
    }
}
