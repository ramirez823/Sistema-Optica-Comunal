using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class Planilla
    {
        public int Id { get; set; }

        // Relación con el empleado (Usuario)
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Mes { get; set; } = string.Empty;

        [Required]
        public int Año { get; set; }

        public DateTime FechaCalculo { get; set; } = DateTime.Now;

        // ===== CAMPOS INGRESADOS POR EL ADMIN =====
        public int HorasBase { get; set; }
        public int TotalHoras { get; set; }
        public int HorasVacaciones { get; set; }
        public int HorasIncapacidadParcial { get; set; }
        public int HorasIncapacidadTotal { get; set; }
        public int HorasPermiso { get; set; }
        public int HorasExtras { get; set; }
        public int HorasDobles { get; set; }

        public decimal Comisiones { get; set; }
        public decimal Prestamos { get; set; }
        public decimal EmbargosPensiones { get; set; }
        public decimal CuentasPorCobrar { get; set; }
        public decimal AdelantoQuincena { get; set; }

        public decimal PorcentajeCCSS { get; set; }
        public decimal PorcentajeSolidarista { get; set; }

        [MaxLength(50)]
        public string? NumeroComprobante { get; set; }

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
    }
}