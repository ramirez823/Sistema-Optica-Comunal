using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class ValorClinico
    {
        public int Id { get; set; }

        [Required]
        public int ExpedienteId { get; set; }
        public Expediente Expediente { get; set; }

        // ========== REFRACCIÓN (Graduación) ==========
        [Required]
        public string Diagnostico { get; set; } = string.Empty;

        // Ojo Derecho (OD)
        public decimal? EsferaOD { get; set; }
        public decimal? CilindroOD { get; set; }
        [Range(0, 180, ErrorMessage = "El eje debe estar entre 0 y 180.")]
        public int? EjeOD { get; set; }

        // Ojo Izquierdo (OI)
        public decimal? EsferaOI { get; set; }
        public decimal? CilindroOI { get; set; }
        [Range(0, 180, ErrorMessage = "El eje debe estar entre 0 y 180.")]
        public int? EjeOI { get; set; }

        // Addition (ADD) — solo para progresivos/bifocales
        [Range(0.75, 3.50, ErrorMessage = "La adición debe estar entre +0.75 y +3.50")]
        public decimal? ADD_Od { get; set; }

        [Range(0.75, 3.50, ErrorMessage = "La adición debe estar entre +0.75 y +3.50")]
        public decimal? ADD_Oi { get; set; }

        // ========== AGUDEZA VISUAL (AV) ==========
        [MaxLength(20)]
        [Display(Name = "AV OD (lejos)")]
        public string? AvOdLejos { get; set; }  // Ej: "20/20", "0.8", "1.0"

        [MaxLength(20)]
        [Display(Name = "AV OI (lejos)")]
        public string? AvOiLejos { get; set; }

        [MaxLength(20)]
        [Display(Name = "AV OD (cerca)")]
        public string? AvOdCerca { get; set; }

        [MaxLength(20)]
        [Display(Name = "AV OI (cerca)")]
        public string? AvOiCerca { get; set; }

        // ========== PRESIÓN INTRAOCULAR (PIO) ==========
        [Display(Name = "PIO OD (mmHg)")]
        [Range(0, 50)]
        public decimal? PioOd { get; set; }

        [Display(Name = "PIO OI (mmHg)")]
        [Range(0, 50)]
        public decimal? PioOi { get; set; }

        // ========== PERCEPCIÓN DE COLORES ==========
        [Display(Name = "Percepción de colores")]
        public string? PercepcionColores { get; set; }  // Ej: "Normal", "Deficiente (Ishihara: 8/14)", "Daltonismo"

        // ========== MOTILIDAD OCULAR Y BALANCE MUSCULAR ==========
        [Display(Name = "Motilidad ocular")]
        public string? MotilidadOcular { get; set; }  // Ej: "Normal", "Limitación a la abducción", "Foria de 2 DP"

        // ========== FONDO DE OJO ==========
        [Display(Name = "Fondo de ojo")]
        public string? FondoOjo { get; set; }  // Ej: "Retina normal", "Papila pálida", "Exudados"

        // ========== CAMPO VISUAL ==========
        [Display(Name = "Campo visual")]
        public string? CampoVisual { get; set; }  // Ej: "Normal", "Escotoma central", "Disminución periférica"

        // ========== OBSERVACIONES GENERALES ==========
        [Display(Name = "Observaciones adicionales")]
        public string? Observaciones { get; set; }

        // ========== METADATOS ==========
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}