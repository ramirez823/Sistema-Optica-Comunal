using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class PlanillaCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un empleado")]
        [Display(Name = "Empleado")]
        public int UsuarioId { get; set; }

        [Display(Name = "Cédula")]
        public string? Cedula { get; set; }

        [Display(Name = "Nombre del Empleado")]
        public string? NombreEmpleado { get; set; }

        [Display(Name = "Salario Base (₡)")]
        public decimal? SalarioBase { get; set; }

        [Required(ErrorMessage = "El mes es requerido")]
        [MaxLength(20)]
        public string Mes { get; set; } = string.Empty;

        [Required]
        public int Año { get; set; }

        [Display(Name = "Número de Comprobante")]
        [MaxLength(50)]
        public string? NumeroComprobante { get; set; }

        // ===== HORAS =====
        [Display(Name = "Horas Base")]
        [Required]
        [Range(0, int.MaxValue)]
        public int HorasBase { get; set; }

        [Display(Name = "Total Horas")]
        [Required]
        [Range(0, int.MaxValue)]
        public int TotalHoras { get; set; }

        [Display(Name = "Horas Vacaciones")]
        public int HorasVacaciones { get; set; }

        [Display(Name = "Horas Incapacidad Parcial")]
        public int HorasIncapacidadParcial { get; set; }

        [Display(Name = "Horas Incapacidad Total")]
        public int HorasIncapacidadTotal { get; set; }

        [Display(Name = "Horas Permiso")]
        public int HorasPermiso { get; set; }

        [Display(Name = "Horas Extras Comunes")]
        public int HorasExtras { get; set; }

        [Display(Name = "Horas Extras Dobles")]
        public int HorasDobles { get; set; }

        // ===== MONTOS =====
        [Display(Name = "Comisiones (₡)")]
        public decimal Comisiones { get; set; }

        [Display(Name = "Préstamos (₡)")]
        public decimal Prestamos { get; set; }

        [Display(Name = "Embargos y Pensiones (₡)")]
        public decimal EmbargosPensiones { get; set; }

        [Display(Name = "Cuentas por Cobrar (₡)")]
        public decimal CuentasPorCobrar { get; set; }

        [Display(Name = "Adelanto de Quincena (₡)")]
        public decimal AdelantoQuincena { get; set; }

        [Display(Name = "Porcentaje CCSS (%)")]
        [Range(0, 100)]
        public decimal PorcentajeCCSS { get; set; }

        [Display(Name = "Porcentaje Asociación Solidarista (%)")]
        [Range(0, 100)]
        public decimal PorcentajeSolidarista { get; set; }

        // ===== CAMPOS CALCULADOS =====
        public decimal SalarioOrdinario { get; set; }
        public decimal ValorHorasExtras { get; set; }
        public decimal ValorHorasDobles { get; set; }
        public decimal ValorVacaciones { get; set; }
        public decimal ValorIncapacidadParcial { get; set; }
        public decimal ValorIncapacidadTotal { get; set; }
        public decimal TotalIngresos { get; set; }

        public decimal MontoCCSS { get; set; }
        public decimal MontoImpuestoRenta { get; set; }
        public decimal MontoSolidarista { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal SalarioNeto { get; set; }

        // Para dropdown
        public IEnumerable<SelectListItem>? EmpleadosList { get; set; }
    }
}