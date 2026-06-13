using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class OrdenTrabajoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un paciente")]
        [Display(Name = "Paciente")]
        public int PacienteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una sucursal")]
        [Display(Name = "Sucursal")]
        public int SucursalId { get; set; }

        [Display(Name = "Venta (opcional)")]
        public int? VentaId { get; set; }

        [Display(Name = "Referencia")]
        [MaxLength(200)]
        public string? Referencia { get; set; }

        [Display(Name = "PD (mm)")]
        [Range(50, 80, ErrorMessage = "El PD debe estar entre 50 y 80 mm")]
        public decimal? PD { get; set; }

        [Display(Name = "Tipo de lente")]
        [MaxLength(100)]
        public string? TipoLente { get; set; }

        [Display(Name = "Material del lente")]
        [MaxLength(100)]
        public string? MaterialLente { get; set; }

        [Display(Name = "Tratamientos")]
        [MaxLength(500)]
        public string? Tratamientos { get; set; }

        [Display(Name = "Laboratorio externo")]
        [MaxLength(200)]
        public string? LaboratorioExterno { get; set; }
    }
}
