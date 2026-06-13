using System;
using System.ComponentModel.DataAnnotations;

namespace OC.Core.Domain.Entities
{
    public class TecnologiaLente
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Precio { get; set; }
    }
}