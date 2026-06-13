using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Services;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Recepcion,Admin")]
    public class SolicitudesCitasController : Controller
    {
        private const int HoraInicio = 8;
        private const int HoraFin = 18;
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;
        private readonly IGenericRepository<Usuario> _usuariosRepo;
        private readonly IGenericRepository<Sucursal> _sucursalesRepo;
        private readonly INotificationService _notificationService;
        private readonly RecordatorioCitasOptions _recordatorioOptions;

        public SolicitudesCitasController(
            IGenericRepository<SolicitudCita> solicitudesRepo,
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Cita> citasRepo,
            IGenericRepository<Usuario> usuariosRepo,
            IGenericRepository<Sucursal> sucursalesRepo,
            INotificationService notificationService,
            IOptions<RecordatorioCitasOptions> recordatorioOptions)
        {
            _solicitudesRepo = solicitudesRepo;
            _pacientesRepo = pacientesRepo;
            _citasRepo = citasRepo;
            _usuariosRepo = usuariosRepo;
            _sucursalesRepo = sucursalesRepo;
            _notificationService = notificationService;
            _recordatorioOptions = recordatorioOptions.Value;
        }

        // LISTAR SOLICITUDES PENDIENTES
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _solicitudesRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: 10,
                filter: s => s.Estado == "Pendiente",
                orderBy: q => q.OrderByDescending(s => s.FechaSolicitud),
                includeProperties: "Paciente"
            );

            // Contar solicitudes pendientes para notificaciones
            var pendientes = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1000,
                filter: s => s.Estado == "Pendiente"
            );
            ViewBag.PendientesCount = pendientes.TotalCount;

            return View(result);
        }

        // Horario 8-18, slots 30 min
        [Authorize(Roles = "Recepcion,Admin,Paciente")]
        [HttpGet]
        public async Task<IActionResult> ObtenerHorasDisponibles(int sucursalId, string fecha)
        {
            if (!DateTime.TryParse(fecha, out var date))
                return Json(Array.Empty<string>());

            var inicioDia = date.Date.AddHours(HoraInicio);
            var finDia = date.Date.AddHours(HoraFin);
            var citasOcupadas = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 500,
                filter: c => c.SucursalId == sucursalId
                    && c.FechaHora >= inicioDia
                    && c.FechaHora < finDia
                    && c.Estado != EstadoCita.Cancelada
            );
            var slotsOcupados = citasOcupadas.Items
                .Select(c => c.FechaHora.ToString("HH:mm"))
                .ToHashSet();

            var disponibles = new List<string>();
            for (int h = HoraInicio; h < HoraFin; h++)
            {
                if (!slotsOcupados.Contains($"{h:D2}:00")) disponibles.Add($"{h:D2}:00");
                if (!slotsOcupados.Contains($"{h:D2}:30")) disponibles.Add($"{h:D2}:30");
            }
            return Json(disponibles);
        }

        [HttpGet]
        public async Task<IActionResult> Agendar(int id)
        {
            var solicitud = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: s => s.Id == id,
                includeProperties: "Paciente"
            );

            var solicitudEntity = solicitud.Items.FirstOrDefault();
            if (solicitudEntity == null) return NotFound();

            var sucursales = await _sucursalesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: s => s.Activo
            );
            var todosUsuarios = await _usuariosRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1000,
                filter: u => u.Activo,
                includeProperties: "Rol"
            );
            var optometristas = todosUsuarios.Items.Where(u => u.Rol.Nombre == "Optometrista");

            var model = new CitaViewModel
            {
                SolicitudCitaId = solicitudEntity.Id,
                PacienteId = solicitudEntity.PacienteId,
                NombrePaciente = solicitudEntity.Paciente.NombreCompleto,
                MotivoSolicitud = solicitudEntity.Motivo,
                FechaHora = ProximaHoraLaboral(DateTime.Now),
                SucursalId = sucursales.Items.FirstOrDefault()?.Id ?? 0,
                SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList(),
                OptometristasList = optometristas.Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre }).ToList()
            };

            return View(model);
        }

        private static DateTime ProximaHoraLaboral(DateTime from)
        {
            var d = from.Date;
            int h = from.Hour;
            if (from.DayOfWeek == DayOfWeek.Sunday) { d = d.AddDays(1); h = HoraInicio; }
            else if (h < HoraInicio) h = HoraInicio;
            else if (h >= HoraFin) { d = d.AddDays(1); if (d.DayOfWeek == DayOfWeek.Sunday) d = d.AddDays(1); h = HoraInicio; }
            return d.AddHours(h);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agendar(CitaViewModel model)
        {
            if (model.SucursalId <= 0)
                ModelState.AddModelError(nameof(model.SucursalId), "Debe seleccionar una sede.");

            var fecha = model.FechaHora;
            if (fecha.Hour < HoraInicio || fecha.Hour >= HoraFin)
                ModelState.AddModelError("", $"El horario de atención es de {HoraInicio}:00 a {HoraFin}:00.");

            if (!ModelState.IsValid)
            {
                await RecargarDropdownsAgendar(model);
                return View(model);
            }

            var ocupado = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 10,
                filter: c => c.SucursalId == model.SucursalId
                    && c.FechaHora == model.FechaHora
                    && c.Estado != EstadoCita.Cancelada
            );
            if (ocupado.Items.Any())
            {
                ModelState.AddModelError("", "Ese horario ya no está disponible en la sede seleccionada. Elija otra hora o sede.");
                await RecargarDropdownsAgendar(model);
                return View(model);
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            int? usuarioAprobadorId = null;
            if (int.TryParse(userIdClaim, out int userId))
                usuarioAprobadorId = userId;

            var solicitud = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: s => s.Id == model.SolicitudCitaId
            );
            var solicitudEntity = solicitud.Items.FirstOrDefault();
            if (solicitudEntity == null) return NotFound();

            solicitudEntity.Estado = "Aprobada";
            solicitudEntity.FechaAprobacion = DateTime.Now;
            solicitudEntity.UsuarioAprobadorId = usuarioAprobadorId;
            await _solicitudesRepo.UpdateAsync(solicitudEntity);

            var cita = new Cita
            {
                PacienteId = model.PacienteId,
                SolicitudCitaId = model.SolicitudCitaId,
                SucursalId = model.SucursalId,
                FechaHora = model.FechaHora,
                MotivoConsulta = model.Observaciones,
                Estado = EstadoCita.Confirmada,
                UsuarioAsignadoId = model.UsuarioAsignadoId,
                FechaCreacion = DateTime.Now
            };

            await _citasRepo.AddAsync(cita);

            // Escenario 2 CIT-RF-016: recordatorio inmediato si la cita es el mismo día y falta menos que el tiempo estándar
            var ahora = DateTime.Now;
            if (cita.FechaHora.Date == ahora.Date && (cita.FechaHora - ahora).TotalHours < _recordatorioOptions.HorasAntesRecordatorio && (cita.FechaHora - ahora).TotalMinutes > 0)
            {
                var citaConIncludes = (await _citasRepo.GetPagedAsync(1, 1, filter: c => c.Id == cita.Id, includeProperties: "Paciente,Sucursal")).Items.FirstOrDefault();
                if (citaConIncludes != null)
                    await _notificationService.EnviarRecordatorioInmediatoAsync(citaConIncludes);
            }

            TempData["Success"] = "Cita agendada exitosamente en la sede seleccionada. El paciente ha sido notificado.";
            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarDropdownsAgendar(CitaViewModel model)
        {
            var sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1, pageSize: 100, filter: s => s.Activo);
            var usuarios = await _usuariosRepo.GetPagedAsync(pageIndex: 1, pageSize: 1000, filter: u => u.Activo, includeProperties: "Rol");
            model.SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList();
            model.OptometristasList = usuarios.Items.Where(u => u.Rol.Nombre == "Optometrista").Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Nombre }).ToList();
        }

        // RECHAZAR SOLICITUD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(int id)
        {
            var solicitud = await _solicitudesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: s => s.Id == id
            );

            var solicitudEntity = solicitud.Items.FirstOrDefault();
            if (solicitudEntity == null) return NotFound();

            solicitudEntity.Estado = "Rechazada";
            solicitudEntity.FechaAprobacion = DateTime.Now;
            await _solicitudesRepo.UpdateAsync(solicitudEntity);

            TempData["Success"] = "Solicitud rechazada";
            return RedirectToAction(nameof(Index));
        }
    }
}
