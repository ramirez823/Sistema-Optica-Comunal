using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Services;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Paciente")]
    public class PacienteDashboardController : Controller
    {
        private const int HoraInicio = 8;
        private const int HoraFin = 18;

        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;
        private readonly IGenericRepository<Sucursal> _sucursalesRepo;
        private readonly IGenericRepository<EnvioNotificacion> _enviosRepo;
        private readonly IGenericRepository<Venta> _ventasRepo;
        private readonly IGenericRepository<OrdenTrabajo> _ordenesRepo;
        private readonly INotificationService _notificationService;
        private readonly RecordatorioCitasOptions _recordatorioOptions;

        public PacienteDashboardController(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Cita> citasRepo,
            IGenericRepository<SolicitudCita> solicitudesRepo,
            IGenericRepository<Sucursal> sucursalesRepo,
            IGenericRepository<EnvioNotificacion> enviosRepo,
            IGenericRepository<Venta> ventasRepo,
            IGenericRepository<OrdenTrabajo> ordenesRepo,
            INotificationService notificationService,
            IOptions<RecordatorioCitasOptions> recordatorioOptions)
        {
            _pacientesRepo = pacientesRepo;
            _citasRepo = citasRepo;
            _solicitudesRepo = solicitudesRepo;
            _sucursalesRepo = sucursalesRepo;
            _enviosRepo = enviosRepo;
            _ventasRepo = ventasRepo;
            _ordenesRepo = ordenesRepo;
            _notificationService = notificationService;
            _recordatorioOptions = recordatorioOptions.Value;
        }

        public async Task<IActionResult> Index()
        {
            // Obtener el ID del paciente desde los Claims
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
            {
                return RedirectToAction("Login", "PacienteAccount");
            }

            // Obtener información del paciente
            var paciente = await _pacientesRepo.GetByIdAsync(pacienteId);
            if (paciente == null)
            {
                return RedirectToAction("Login", "PacienteAccount");
            }

            // Obtener citas del paciente
            try
            {
                var citas = await _citasRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 100,
                    filter: c => c.PacienteId == pacienteId,
                    orderBy: q => q.OrderByDescending(c => c.FechaHora),
                    includeProperties: "Paciente,Sucursal"
                );

                var ahora = DateTime.Now;
                var recordatorios = citas.Items
                    .Where(c => c.Estado != EstadoCita.Cancelada
                        && c.FechaHora >= ahora
                        && c.FechaHora <= ahora.AddHours(48))
                    .OrderBy(c => c.FechaHora)
                    .ToList();
                // Citas canceladas recientes (para mostrar aviso tipo "Atención: su cita fue cancelada")
                var canceladasRecientes = citas.Items
                    .Where(c => c.Estado == EstadoCita.Cancelada && c.FechaHora >= ahora.AddDays(-14))
                    .OrderByDescending(c => c.FechaHora)
                    .ToList();
                // Escenario 2: citas agendadas para hoy (aviso amarillo)
                var citasHoy = citas.Items
                    .Where(c => c.Estado != EstadoCita.Cancelada && c.FechaHora.Date == ahora.Date && c.FechaHora >= ahora)
                    .OrderBy(c => c.FechaHora)
                    .ToList();

                ViewBag.Paciente = paciente;
                ViewBag.Citas = citas.Items;
                ViewBag.Recordatorios = recordatorios;
                ViewBag.CanceladasRecientes = canceladasRecientes;
                ViewBag.CitasHoy = citasHoy;
            }
            catch
            {
                // Si la tabla no existe aún, mostrar lista vacía
                ViewBag.Paciente = paciente;
                ViewBag.Citas = new List<Cita>();
                ViewBag.Recordatorios = new List<Cita>();
                ViewBag.CanceladasRecientes = new List<Cita>();
                ViewBag.CitasHoy = new List<Cita>();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Notificaciones()
        {
            // Obtener el ID del paciente desde los Claims
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            // Traer notificaciones de OT (lentes listos) asociadas al paciente
            // Nota: filtramos por navegación OrdenTrabajo.PacienteId (include para mostrar datos).
            var envios = await _enviosRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 200,
                filter: e => e.OrdenTrabajoId != null && e.OrdenTrabajo.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(e => e.FechaHoraEnvio),
                includeProperties: "OrdenTrabajo,OrdenTrabajo.Sucursal"
            );

            return View(envios.Items);
        }

        [HttpGet]
        public async Task<IActionResult> MisFacturas()
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            var ventas = await _ventasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 200,
                filter: v => v.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(v => v.FechaVenta)
            );

            return View(ventas.Items);
        }

        [HttpGet]
        public async Task<IActionResult> EstadoOrden()
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            // Escenario 1: al entrar al módulo, mostrar órdenes del paciente y su estado actualizado.
            var ordenes = await _ordenesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: o => o.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(o => o.FechaCreacion),
                includeProperties: "Sucursal"
            );

            ViewBag.Ordenes = ordenes.Items;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EstadoOrdenDetalle(int id)
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            var ordenResult = await _ordenesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: o => o.Id == id && o.PacienteId == pacienteId,
                includeProperties: "Sucursal"
            );
            var orden = ordenResult.Items.FirstOrDefault();
            if (orden == null)
            {
                TempData["Error"] = "La orden no está lista o no existe.";
                return RedirectToAction(nameof(EstadoOrden));
            }

            return View("EstadoOrdenDetalle", orden);
        }

        // Horarios disponibles (slots 30 min) por sede y fecha
        [HttpGet]
        public async Task<IActionResult> ObtenerHorasDisponibles(int sucursalId, string fecha)
        {
            if (sucursalId <= 0 || string.IsNullOrWhiteSpace(fecha))
                return Json(Array.Empty<string>());
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
        public async Task<IActionResult> SolicitarCita()
        {
            var sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1, pageSize: 100, filter: s => s.Activo);
            ViewBag.SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList();
            ViewBag.ServiciosList = new[]
            {
                new SelectListItem { Value = "Examen visual", Text = "Examen visual" },
                new SelectListItem { Value = "Control de lentes", Text = "Control de lentes" },
                new SelectListItem { Value = "Adaptación de lentes de contacto", Text = "Adaptación de lentes de contacto" },
                new SelectListItem { Value = "Consulta general", Text = "Consulta general" }
            };
            ViewBag.EsSolicitarCita = true;
            return View("AgendarCita");
        }

        [HttpGet]
        public async Task<IActionResult> AgendarCita()
        {
            var sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1, pageSize: 100, filter: s => s.Activo);
            ViewBag.SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList();
            ViewBag.ServiciosList = new[]
            {
                new SelectListItem { Value = "Examen visual", Text = "Examen visual" },
                new SelectListItem { Value = "Control de lentes", Text = "Control de lentes" },
                new SelectListItem { Value = "Adaptación de lentes de contacto", Text = "Adaptación de lentes de contacto" },
                new SelectListItem { Value = "Consulta general", Text = "Consulta general" }
            };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgendarCita(int sucursalId, string fecha, string hora, string servicio, string? motivo)
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            var faltantes = new List<string>();
            if (sucursalId <= 0) faltantes.Add("sede");
            if (string.IsNullOrWhiteSpace(fecha)) faltantes.Add("fecha");
            if (string.IsNullOrWhiteSpace(hora)) faltantes.Add("horario");
            if (string.IsNullOrWhiteSpace(servicio)) faltantes.Add("servicio");
            if (faltantes.Any())
            {
                TempData["Error"] = $"Complete los campos requeridos: {string.Join(", ", faltantes)}.";
                return RedirectToAction(nameof(AgendarCita));
            }

            if (!DateTime.TryParse(fecha, out var date))
            {
                TempData["Error"] = "La fecha ingresada no es válida.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var parts = hora.Trim().Split(':');
            if (parts.Length < 2 || !int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m))
            {
                TempData["Error"] = "Hora no válida.";
                return RedirectToAction(nameof(AgendarCita));
            }
            if (h < HoraInicio || h >= HoraFin || (m != 0 && m != 30))
            {
                TempData["Error"] = "El horario es de 8:00 a 18:00 en slots de 30 minutos.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var fechaHora = date.Date.AddHours(h).AddMinutes(m);

            var ocupado = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 10,
                filter: c => c.SucursalId == sucursalId && c.FechaHora == fechaHora && c.Estado != EstadoCita.Cancelada
            );
            if (ocupado.Items.Any())
            {
                TempData["Error"] = "El horario seleccionado ya no está disponible.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var solicitud = new SolicitudCita
            {
                PacienteId = pacienteId,
                Motivo = motivo ?? "Reserva en línea",
                FechaSolicitud = DateTime.Now,
                Estado = "Aprobada"
            };
            await _solicitudesRepo.AddAsync(solicitud);

            var cita = new Cita
            {
                PacienteId = pacienteId,
                SolicitudCitaId = solicitud.Id,
                SucursalId = sucursalId,
                FechaHora = fechaHora,
                MotivoConsulta = string.IsNullOrWhiteSpace(motivo) ? servicio : $"{servicio}. {motivo}",
                Estado = EstadoCita.Confirmada,
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

            TempData["Success"] = $"Cita agendada correctamente para el {fechaHora:dd/MM/yyyy HH:mm}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditarMiCita(int id)
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            var citaResult = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: c => c.Id == id && c.PacienteId == pacienteId,
                includeProperties: "Sucursal"
            );
            var cita = citaResult.Items.FirstOrDefault();
            if (cita == null)
                return NotFound();

            if (cita.Estado != EstadoCita.Confirmada && cita.Estado != EstadoCita.Pendiente)
            {
                TempData["Error"] = "Solo puede cambiar la fecha/hora de citas en estado Confirmada o Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            var sucursales = await _sucursalesRepo.GetPagedAsync(pageIndex: 1, pageSize: 100, filter: s => s.Activo);
            ViewBag.Cita = cita;
            ViewBag.SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMiCita(int id, int sucursalId, string fecha, string hora)
        {
            var pacienteIdClaim = User.FindFirst("PacienteId")?.Value;
            if (!int.TryParse(pacienteIdClaim, out int pacienteId))
                return RedirectToAction("Login", "PacienteAccount");

            var citaResult = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: c => c.Id == id && c.PacienteId == pacienteId
            );
            var cita = citaResult.Items.FirstOrDefault();
            if (cita == null)
                return NotFound();

            if (cita.Estado != EstadoCita.Confirmada && cita.Estado != EstadoCita.Pendiente)
            {
                TempData["Error"] = "Solo puede cambiar la fecha/hora de citas en estado Confirmada o Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            if (!DateTime.TryParse(fecha, out var date) || string.IsNullOrWhiteSpace(hora))
            {
                TempData["Error"] = "Seleccione fecha y hora.";
                return RedirectToAction(nameof(EditarMiCita), new { id });
            }

            var parts = hora.Trim().Split(':');
            if (parts.Length < 2 || !int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m))
            {
                TempData["Error"] = "Hora no válida.";
                return RedirectToAction(nameof(EditarMiCita), new { id });
            }
            if (h < HoraInicio || h >= HoraFin || (m != 0 && m != 30))
            {
                TempData["Error"] = "El horario es de 8:00 a 18:00 en slots de 30 minutos.";
                return RedirectToAction(nameof(EditarMiCita), new { id });
            }

            var nuevaFechaHora = date.Date.AddHours(h).AddMinutes(m);

            var ocupado = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 10,
                filter: c => c.SucursalId == sucursalId && c.FechaHora == nuevaFechaHora && c.Estado != EstadoCita.Cancelada && c.Id != id
            );
            if (ocupado.Items.Any())
            {
                TempData["Error"] = "Ese horario ya no está disponible. Elija otro slot.";
                return RedirectToAction(nameof(EditarMiCita), new { id });
            }

            cita.SucursalId = sucursalId;
            cita.FechaHora = nuevaFechaHora;
            await _citasRepo.UpdateAsync(cita);

            TempData["Success"] = "Cita actualizada. El horario anterior quedó liberado para otros pacientes.";
            return RedirectToAction(nameof(Index));
        }
    }
}
