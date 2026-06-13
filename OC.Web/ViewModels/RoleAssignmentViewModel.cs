using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OC.Web.ViewModels
{
    public class RoleAssignmentViewModel
    {
        [Display(Name = "Usuario")]
        [Required(ErrorMessage = "Debe seleccionar un Usuario")]
        public int? UserId { get; set; }

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "Debe seleccionar un Rol")]
        public int? RoleId { get; set; }

        public IEnumerable<SelectListItem>? UsersList { get; set; }
        public IEnumerable<SelectListItem>? RolesList { get; set; }
    }
}