using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class DocumentoExpedienteViewModel
    {
        public int ExpedienteId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un archivo.")]
        [Display(Name = "Archivo")]
        public IFormFile Archivo { get; set; }

        [Display(Name = "Descripción (opcional)")]
        public string? Descripcion { get; set; }
    }
}