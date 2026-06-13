using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Helpers;
using OC.Web.ViewModels;
using OC.Web.Services;
using System.Security.Claims;
using System.Linq.Expressions;

namespace OC.Web.Controllers
{
    [AllowAnonymous]
    public class CitasPublicasController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudesRepo;
        private readonly IGenericRepository<Cita> _citasRepo;
        private readonly IGenericRepository<Expediente> _expedienteRepo;
        private readonly INotificationService _notificationService;

        public CitasPublicasController(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<SolicitudCita> solicitudesRepo,
            IGenericRepository<Cita> citasRepo,
            IGenericRepository<Expediente> expedienteRepo,
            INotificationService notificationService)
        {
            _pacientesRepo = pacientesRepo;
            _solicitudesRepo = solicitudesRepo;
            _citasRepo = citasRepo;
            _expedienteRepo = expedienteRepo;
            _notificationService = notificationService;
        }

        // SOLICITAR CITA (Público)
        [HttpGet]
        public IActionResult Solicitar() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Solicitar(string cedula, string motivo)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View();
            }

            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm
            );
            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null)
            {
                ModelState.AddModelError("", "No se encontró un paciente con esa cédula. Por favor, regístrese primero.");
                return View();
            }

            var solicitud = new SolicitudCita
            {
                PacienteId = paciente.Id,
                Motivo = motivo,
                FechaSolicitud = DateTime.Now,
                Estado = "Pendiente"
            };
            await _solicitudesRepo.AddAsync(solicitud);

            TempData["Success"] = "Solicitud de cita enviada exitosamente. Un recepcionista se pondrá en contacto con usted.";
            return RedirectToAction(nameof(Solicitar));
        }

        // VER MIS CITAS (Público - por cédula)
        [HttpGet]
        public IActionResult VerMisCitas() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerMisCitas(string cedula)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View();
            }

            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm,
                includeProperties: "Citas"
            );
            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null)
            {
                ModelState.AddModelError("", "No se encontró un paciente con esa cédula.");
                return View();
            }

            var citas = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: c => c.PacienteId == paciente.Id,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Paciente"
            );

            ViewBag.Paciente = paciente;
            ViewBag.Citas = citas.Items;
            return View();
        }

        [Authorize]
        public async Task<IActionResult> CitasPaciente(
            string? search = null,
            string? estado = null,
            string? fechaDesde = null,
            string? fechaHasta = null,
            string? sort = null,
            int page = 1,
            int pageSize = 15)
        {
            ViewBag.Search = search;
            ViewBag.Estado = estado;
            ViewBag.FechaDesde = fechaDesde;
            ViewBag.FechaHasta = fechaHasta;
            ViewBag.Sort = sort;

            // Parsear fechas fuera del Expression Tree (out var no se admite en expression trees).
            DateTime? fDesde = null;
            DateTime? fHasta = null;
            if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out var d1)) fDesde = d1.Date;
            if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out var d2)) fHasta = d2.Date;

            // Default: descending (newest first)
            Func<IQueryable<Cita>, IOrderedQueryable<Cita>> orderBy = q => q.OrderByDescending(c => c.FechaHora);

            if (sort == "asc")
                orderBy = q => q.OrderBy(c => c.FechaHora);
            else if (sort == "patient")
                orderBy = q => q.OrderBy(c => c.Paciente != null ? c.Paciente.Nombres : "");
            else if (sort == "patient_desc")
                orderBy = q => q.OrderByDescending(c => c.Paciente != null ? c.Paciente.Nombres : "");

            Expression<Func<Cita, bool>>? filter = null;

            // Combine all WHERE clauses into a single expression so the repository
            // can push everything down to SQL (no in-memory filtering).
            if (!string.IsNullOrEmpty(estado) ||
                fDesde.HasValue ||
                fHasta.HasValue ||
                !string.IsNullOrWhiteSpace(search))
            {
                var term = (search ?? string.Empty).Trim().ToLower();
                var estadoFiltro = estado ?? string.Empty;
                int? idBusqueda = null;
                if (int.TryParse(term, out var idNum))
                    idBusqueda = idNum;

                filter = c =>
                    (estadoFiltro == string.Empty || c.Estado == estadoFiltro) &&
                    (!fDesde.HasValue || c.FechaHora.Date >= fDesde.Value) &&
                    (!fHasta.HasValue || c.FechaHora.Date <= fHasta.Value) &&
                    (string.IsNullOrWhiteSpace(search) ||
                        (c.Paciente != null &&
                            (c.Paciente.Nombres.ToLower().Contains(term) ||
                             c.Paciente.Apellidos.ToLower().Contains(term))) ||
                        (c.Paciente != null && c.Paciente.Cedula != null && c.Paciente.Cedula.ToLower().Contains(term)) ||
                        (idBusqueda.HasValue && c.Id == idBusqueda.Value));
            }

            var resultado = await _citasRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: pageSize,
                filter: filter,
                orderBy: orderBy,
                includeProperties: "Paciente"
            );

            return View(resultado);
        }

        [Authorize(Roles = "Optometrista, Recepcion, Admin")]
        public async Task<IActionResult> Editar(int id)
        {
            var cita = (await _citasRepo.GetPagedAsync(
                1, 1,
                filter: c => c.Id == id,
                includeProperties: "Paciente,Expediente"
            )).Items.FirstOrDefault();

            if (cita == null) return NotFound();
            return View(cita);
        }

        [HttpPost]
        [Authorize(Roles = "Optometrista, Recepcion, Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string estado, string resultadoClinico)
        {
            var esRecepcionista = User.IsInRole("Recepcion");
            var cita = (await _citasRepo.GetPagedAsync(
                1, 1,
                filter: c => c.Id == id,
                includeProperties: "Paciente"
            )).Items.FirstOrDefault();

            if (esRecepcionista && estado != EstadoCita.Cancelada)
            {
                ModelState.AddModelError("", "Recepción solo puede cancelar citas.");
                return View(cita);
            }

            if (cita == null) return NotFound();

            if (estado == EstadoCita.Atendida && string.IsNullOrWhiteSpace(resultadoClinico))
            {
                ModelState.AddModelError("", "Debe ingresar resultado clínico.");
                return View(cita);
            }

            cita.Estado = estado;
            if (estado == EstadoCita.Atendida)
            {
                cita.ObservacionesEspecialista = resultadoClinico;
            }

            await _citasRepo.UpdateAsync(cita);

            // Escenario 3 CIT-RF-016: notificar cancelación al paciente
            if (estado == EstadoCita.Cancelada)
                await _notificationService.EnviarNotificacionCancelacionAsync(cita);

            TempData["Success"] = "Cita actualizada correctamente.";

            if (estado == EstadoCita.Atendida && (User.IsInRole("Optometrista") || User.IsInRole("Admin")))
            {
                return RedirectToAction("Create", "Expedientess", new { citaId = cita.Id });
            }
            else
            {
                return RedirectToAction("CitasPaciente");
            }
        }

        [Authorize]
        public async Task<IActionResult> MiHistorial()
        {
            var userCedula = User.FindFirst("Cedula")?.Value;
            var paciente = (await _pacientesRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Cedula == userCedula
            )).Items.FirstOrDefault();

            if (paciente == null)
                return RedirectToAction("Index", "Home");

            var citas = await _citasRepo.GetPagedAsync(
                1, 200,
                filter: c => c.PacienteId == paciente.Id && c.Estado == EstadoCita.Atendida,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Paciente,UsuarioAsignado,Expediente"
            );
            return View(citas.Items);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DetalleHistorial(int id)
        {
            var userCedula = User.FindFirst("Cedula")?.Value;
            var paciente = (await _pacientesRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Cedula == userCedula
            )).Items.FirstOrDefault();

            if (paciente == null)
                return RedirectToAction("Index", "Home");

            var citaResult = await _citasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: c => c.Id == id && c.PacienteId == paciente.Id,
                includeProperties: "Sucursal,UsuarioAsignado,Expediente,Expediente.ValoresClinicos,Expediente.Documentos"
            );
            var cita = citaResult.Items.FirstOrDefault();
            if (cita == null) return NotFound();

            return View("DetalleHistorial", cita);
        }

        // NUEVA ACCIÓN: Historial de expedientes (misma URL, pero ahora devuelve expedientes)
        [Authorize(Roles = "Optometrista,Admin,Recepcion")]
        public async Task<IActionResult> HistorialPaciente(int pacienteId)
        {
            var expedientes = await _expedienteRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: e => e.Cita.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(e => e.FechaRegistro),
                includeProperties: "Cita.Paciente,ValoresClinicos,Documentos"
            );

            if (!expedientes.Items.Any())
            {
                TempData["Info"] = "Este paciente no tiene expedientes registrados.";
            }

            return View("~/Views/Expedientess/HistorialPaciente.cshtml", expedientes.Items);
        }
    }
}
