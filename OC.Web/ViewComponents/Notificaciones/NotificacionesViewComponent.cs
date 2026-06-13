using Microsoft.AspNetCore.Mvc;

namespace OC.Web.ViewComponents.Notificaciones
{
    public class NotificacionesViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync()
        {
            // Solicitudes retiradas del menú; pacientes agendan directo
            return Task.FromResult<IViewComponentResult>(View(0));
        }
    }
}
