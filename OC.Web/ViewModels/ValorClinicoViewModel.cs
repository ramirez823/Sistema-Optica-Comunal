using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class ValorClinicoViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ExpedienteId { get; set; }

        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        [Display(Name = "Diagnóstico")]
        [MaxLength(500, ErrorMessage = "El diagnóstico no puede exceder 500 caracteres.")]
        public string Diagnostico { get; set; } = string.Empty;

        // ========== REFRACCIÓN ==========
        [Display(Name = "Esfera Ojo Derecho")]
        public decimal? EsferaOD { get; set; }

        [Display(Name = "Cilindro Ojo Derecho")]
        public decimal? CilindroOD { get; set; }

        [Display(Name = "Eje Ojo Derecho")]
        [Range(0, 180, ErrorMessage = "El eje debe estar entre 0 y 180.")]
        public int? EjeOD { get; set; }

        [Display(Name = "Esfera Ojo Izquierdo")]
        public decimal? EsferaOI { get; set; }

        [Display(Name = "Cilindro Ojo Izquierdo")]
        public decimal? CilindroOI { get; set; }

        [Display(Name = "Eje Ojo Izquierdo")]
        [Range(0, 180, ErrorMessage = "El eje debe estar entre 0 y 180.")]
        public int? EjeOI { get; set; }

        [Display(Name = "Adición OD")]
        [Range(0.75, 3.50, ErrorMessage = "La adición debe estar entre +0.75 y +3.50")]
        public decimal? ADD_Od { get; set; }

        [Display(Name = "Adición OI")]
        [Range(0.75, 3.50, ErrorMessage = "La adición debe estar entre +0.75 y +3.50")]
        public decimal? ADD_Oi { get; set; }

        // ========== AGUDEZA VISUAL ==========
        [Display(Name = "AV OD (lejos)")]
        [MaxLength(20)]
        public string? AvOdLejos { get; set; }

        [Display(Name = "AV OI (lejos)")]
        [MaxLength(20)]
        public string? AvOiLejos { get; set; }

        [Display(Name = "AV OD (cerca)")]
        [MaxLength(20)]
        public string? AvOdCerca { get; set; }

        [Display(Name = "AV OI (cerca)")]
        [MaxLength(20)]
        public string? AvOiCerca { get; set; }

        // ========== PRESIÓN INTRAOCULAR ==========
        [Display(Name = "PIO OD (mmHg)")]
        [Range(0, 50)]
        public decimal? PioOd { get; set; }

        [Display(Name = "PIO OI (mmHg)")]
        [Range(0, 50)]
        public decimal? PioOi { get; set; }

        // ========== PERCEPCIÓN DE COLORES ==========
        [Display(Name = "Percepción de colores")]
        public string? PercepcionColores { get; set; }

        // ========== MOTILIDAD OCULAR ==========
        [Display(Name = "Motilidad ocular")]
        public string? MotilidadOcular { get; set; }

        // ========== FONDO DE OJO ==========
        [Display(Name = "Fondo de ojo")]
        public string? FondoOjo { get; set; }

        // ========== CAMPO VISUAL ==========
        [Display(Name = "Campo visual")]
        public string? CampoVisual { get; set; }

        // ========== OBSERVACIONES ==========
        [Display(Name = "Observaciones adicionales")]
        public string? Observaciones { get; set; }

        // ========== DATOS DE LA CITA (solo visualización) ==========
        public string? NombrePaciente { get; set; }
        public string? MotivoConsulta { get; set; }
    }
}