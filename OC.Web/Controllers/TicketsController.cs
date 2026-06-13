using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Core.Services;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly IGenericRepository<Ticket> _ticketRepo;
        private readonly IGenericRepository<Equipo> _equipoRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;

        public TicketsController(
            IGenericRepository<Ticket> ticketRepo,
            IGenericRepository<Equipo> equipoRepo,
            IGenericRepository<Usuario> usuarioRepo)
        {
            _ticketRepo = ticketRepo;
            _equipoRepo = equipoRepo;
            _usuarioRepo = usuarioRepo;
        }

        #region Métodos auxiliares (implementaciones reales)

        private async Task<IEnumerable<SelectListItem>> ObtenerEquipos()
        {
            var equipos = await _equipoRepo.GetPagedAsync(1, 100, filter: e => e.Activo, orderBy: q => q.OrderBy(e => e.Nombre));
            return equipos.Items.Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.Nombre} ({e.Tipo})"
            });
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerTecnicos(int? seleccionado = null)
        {
            var tecnicos = await _usuarioRepo.GetPagedAsync(1, 100,
                filter: u => u.Activo && u.Rol != null && u.Rol.Nombre == "Tecnico",
                includeProperties: "Rol",
                orderBy: q => q.OrderBy(u => u.Nombre));

            return tecnicos.Items.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.Nombre,
                Selected = (seleccionado.HasValue && u.Id == seleccionado.Value)
            });
        }

        private List<SelectListItem> ObtenerTipos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Hardware", Text = "Hardware" },
                new SelectListItem { Value = "Software", Text = "Software" },
                new SelectListItem { Value = "Red", Text = "Red" },
                new SelectListItem { Value = "Periférico", Text = "Periférico" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
        }

        private List<SelectListItem> ObtenerPrioridades()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Baja", Text = "Baja" },
                new SelectListItem { Value = "Media", Text = "Media" },
                new SelectListItem { Value = "Alta", Text = "Alta" },
                new SelectListItem { Value = "Urgente", Text = "Urgente" }
            };
        }

        private List<SelectListItem> ObtenerEstados()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Asignado", Text = "Asignado" },
                new SelectListItem { Value = "En Proceso", Text = "En Proceso" },
                new SelectListItem { Value = "Resuelto", Text = "Resuelto" },
                new SelectListItem { Value = "Cerrado", Text = "Cerrado" }
            };
        }

        private List<SelectListItem> ObtenerSatisfaccion()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "5", Text = "⭐⭐⭐⭐⭐ Excelente" },
                new SelectListItem { Value = "4", Text = "⭐⭐⭐⭐ Bueno" },
                new SelectListItem { Value = "3", Text = "⭐⭐⭐ Regular" },
                new SelectListItem { Value = "2", Text = "⭐⭐ Malo" },
                new SelectListItem { Value = "1", Text = "⭐ Pésimo" }
            };
        }

        private void CalcularYAsignarSLA(Ticket ticket)
        {
            if (!string.IsNullOrEmpty(ticket.Prioridad))
            {
                var (respuestaEsperada, resolucionEsperada) = SLAService.CalcularFechasSLA(ticket.Prioridad);
                ticket.FechaRespuestaEsperada = respuestaEsperada;
                ticket.FechaResolucionEsperada = resolucionEsperada;
            }
        }

        #endregion

        #region Acciones para todos los usuarios

        // GET: Tickets/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new TicketCreateViewModel
            {
                EquiposList = await ObtenerEquipos()
            };
            return View(viewModel);
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.EquiposList = await ObtenerEquipos();
                return View(model);
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var ultimoTicket = (await _ticketRepo.GetPagedAsync(1, 1, orderBy: q => q.OrderByDescending(t => t.Id)))
                .Items.FirstOrDefault();
            int nuevoNum = (ultimoTicket?.Id ?? 0) + 1;
            var numeroSeguimiento = $"TKT-{fecha}-{nuevoNum:D4}";

            var ticket = new Ticket
            {
                NumeroSeguimiento = numeroSeguimiento,
                Titulo = model.Titulo,
                Descripcion = model.Descripcion,
                Estado = "Pendiente",
                CreadoPorId = userId,
                EquipoId = model.EquipoId,
                FechaCreacion = DateTime.Now
            };

            await _ticketRepo.AddAsync(ticket);

            TempData["Success"] = $"Ticket {numeroSeguimiento} creado exitosamente.";
            return RedirectToAction(nameof(MisTickets));
        }

        // GET: Tickets/MisTickets
        public async Task<IActionResult> MisTickets(int page = 1)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            var tickets = await _ticketRepo.GetPagedAsync(
                page, 15,
                filter: t => t.CreadoPorId == userId,
                orderBy: q => q.OrderByDescending(t => t.FechaCreacion),
                includeProperties: "Equipo,TecnicoAsignado"
            );

            ViewBag.TicketsResueltos = tickets.Items.Where(t => t.Estado == "Resuelto").ToList();
            ViewBag.TicketsActivos = tickets.Items.Where(t => t.Estado != "Resuelto").ToList();

            return View(tickets);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo,TecnicoAsignado"
            )).Items.FirstOrDefault();

            if (ticket == null)
                return NotFound();

            return View(ticket);
        }

        #endregion

        #region Acciones para Técnico y Admin

        [Authorize(Roles = "Tecnico,Admin")]
        public async Task<IActionResult> PanelTecnico(int page = 1)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int userId);

            var tickets = await _ticketRepo.GetPagedAsync(
                page, 20,
                filter: t => t.Estado != "Cerrado" && t.Estado != "Resuelto" && (t.TecnicoAsignadoId == null || t.TecnicoAsignadoId == userId),
                orderBy: q => q.OrderBy(t => t.FechaCreacion),
                includeProperties: "CreadoPor,Equipo,TecnicoAsignado"
            );

            return View(tickets);
        }

        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TomarTicket(int id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.TecnicoAsignadoId == null)
            {
                ticket.TecnicoAsignadoId = userId;
                ticket.Estado = "Asignado";
                ticket.FechaAsignacion = DateTime.Now;

                if (!string.IsNullOrEmpty(ticket.Prioridad))
                {
                    var (respuestaEsperada, resolucionEsperada) = SLAService.CalcularFechasSLA(ticket.Prioridad);
                    ticket.FechaRespuestaEsperada = respuestaEsperada;
                    ticket.FechaResolucionEsperada = resolucionEsperada;
                }

                await _ticketRepo.UpdateAsync(ticket);
                TempData["Success"] = "Ticket asignado a usted correctamente.";
            }
            else if (ticket.TecnicoAsignadoId == userId)
            {
                TempData["Info"] = "Este ticket ya está asignado a usted.";
            }
            else
            {
                TempData["Error"] = "Este ticket ya está asignado a otro técnico.";
            }

            return RedirectToAction(nameof(PanelTecnico));
        }

        [Authorize(Roles = "Tecnico,Admin")]
        public async Task<IActionResult> Clasificar(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo"
            )).Items.FirstOrDefault();

            if (ticket == null) return NotFound();

            var viewModel = new TicketClasificarViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo,
                Tipo = ticket.Tipo ?? "",
                Prioridad = ticket.Prioridad ?? "",
                TiposList = ObtenerTipos(),
                PrioridadesList = ObtenerPrioridades()
            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clasificar(TicketClasificarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.TiposList = ObtenerTipos();
                model.PrioridadesList = ObtenerPrioridades();
                return View(model);
            }

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            var prioridadAnterior = ticket.Prioridad;
            ticket.Tipo = model.Tipo;
            ticket.Prioridad = model.Prioridad;

            if (prioridadAnterior != ticket.Prioridad)
            {
                CalcularYAsignarSLA(ticket);
            }

            if (ticket.Estado == "Pendiente" && ticket.TecnicoAsignadoId != null)
                ticket.Estado = "Asignado";

            await _ticketRepo.UpdateAsync(ticket);
            TempData["Success"] = "Ticket clasificado correctamente.";
            return RedirectToAction(nameof(PanelTecnico));
        }

        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarRespuesta(int id, string? comentario = null)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return RedirectToAction("Login", "Account");

            if (ticket.TecnicoAsignadoId != userId)
            {
                TempData["Error"] = "Solo el técnico asignado puede registrar la primera respuesta.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!ticket.FechaPrimeraRespuesta.HasValue)
            {
                ticket.FechaPrimeraRespuesta = DateTime.Now;
                ticket.SLA_CumplidoRespuesta = ticket.FechaPrimeraRespuesta <= ticket.FechaRespuestaEsperada;

                if (!ticket.SLA_CumplidoRespuesta)
                {
                    ticket.SLA_Observacion = $"Respuesta fuera de plazo. Esperada: {ticket.FechaRespuestaEsperada?.ToString("dd/MM/yyyy HH:mm")}";
                }

                ticket.Estado = "En Proceso";
                await _ticketRepo.UpdateAsync(ticket);
                TempData["Success"] = "Primera respuesta registrada correctamente.";
            }
            else
            {
                TempData["Info"] = "Este ticket ya tiene una respuesta registrada.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Tecnico,Admin")]
        public async Task<IActionResult> Resolver(int id)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int userId);
            if (ticket.TecnicoAsignadoId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var viewModel = new TicketResolverViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo,
                FechaCreacion = ticket.FechaCreacion,
                FechaAsignacion = ticket.FechaAsignacion
            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Tecnico,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolver(TicketResolverViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdClaim, out int userId);
            if (ticket.TecnicoAsignadoId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (ticket.Estado == "Resuelto" || ticket.Estado == "Cerrado")
            {
                TempData["Error"] = "Este ticket ya está resuelto o cerrado.";
                return RedirectToAction(nameof(PanelTecnico));
            }

            ticket.Estado = "Resuelto";
            ticket.FechaResolucion = DateTime.Now;
            ticket.SolucionAplicada = model.SolucionAplicada;
            ticket.ObservacionesInternas = model.ObservacionesInternas;

            var inicio = ticket.FechaAsignacion ?? ticket.FechaCreacion;
            var horas = (ticket.FechaResolucion.Value - inicio).TotalHours;
            ticket.TiempoDedicado = $"{Math.Floor(horas)}h {(horas % 1) * 60:F0}m";

            // Calcular cumplimiento de SLA de resolución basado en la fecha de resolución (no cierre)
            if (ticket.FechaResolucionEsperada.HasValue)
            {
                ticket.SLA_CumplidoResolucion = ticket.FechaResolucion.Value <= ticket.FechaResolucionEsperada;
            }

            await _ticketRepo.UpdateAsync(ticket);

            TempData["Success"] = "Ticket marcado como resuelto. El cliente podrá calificarlo.";
            return RedirectToAction(nameof(PanelTecnico));
        }

        #endregion

        #region Acciones para Admin

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int page = 1, string? estado = null, string? prioridad = null)
        {
            var tickets = await _ticketRepo.GetPagedAsync(
                page, 15,
                filter: t =>
                    (string.IsNullOrEmpty(estado) || t.Estado == estado) &&
                    (string.IsNullOrEmpty(prioridad) || t.Prioridad == prioridad),
                orderBy: q => q.OrderByDescending(t => t.FechaCreacion),
                includeProperties: "CreadoPor,TecnicoAsignado,Equipo"
            );

            ViewBag.EstadoFiltro = estado;
            ViewBag.PrioridadFiltro = prioridad;
            ViewBag.EstadosList = ObtenerEstados();
            ViewBag.PrioridadesList = ObtenerPrioridades();

            return View(tickets);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Asignar(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo"
            )).Items.FirstOrDefault();

            if (ticket == null) return NotFound();

            var viewModel = new TicketAsignarViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo,
                TecnicoAsignadoId = ticket.TecnicoAsignadoId,
                TecnicosList = await ObtenerTecnicos(ticket.TecnicoAsignadoId)
            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(TicketAsignarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.TecnicosList = await ObtenerTecnicos(model.TecnicoAsignadoId);
                return View(model);
            }

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            ticket.TecnicoAsignadoId = model.TecnicoAsignadoId;
            ticket.Estado = "Asignado";
            ticket.FechaAsignacion = DateTime.Now;

            if (!string.IsNullOrEmpty(ticket.Prioridad))
            {
                var (respuestaEsperada, resolucionEsperada) = SLAService.CalcularFechasSLA(ticket.Prioridad);
                ticket.FechaRespuestaEsperada = respuestaEsperada;
                ticket.FechaResolucionEsperada = resolucionEsperada;
            }

            await _ticketRepo.UpdateAsync(ticket);

            TempData["Success"] = "Ticket asignado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = (await _ticketRepo.GetPagedAsync(
                1, 1,
                filter: t => t.Id == id,
                includeProperties: "CreadoPor,Equipo,TecnicoAsignado"
            )).Items.FirstOrDefault();

            if (ticket == null) return NotFound();

            var viewModel = new TicketEditViewModel
            {
                Id = ticket.Id,
                NumeroSeguimiento = ticket.NumeroSeguimiento,
                Titulo = ticket.Titulo,
                Descripcion = ticket.Descripcion,
                Estado = ticket.Estado,
                Prioridad = ticket.Prioridad ?? "",
                Tipo = ticket.Tipo,
                TecnicoAsignadoId = ticket.TecnicoAsignadoId,
                EquipoId = ticket.EquipoId,
                EstadosList = ObtenerEstados(),
                TiposList = ObtenerTipos(),
                PrioridadesList = ObtenerPrioridades(),
                TecnicosList = await ObtenerTecnicos(ticket.TecnicoAsignadoId),
                EquiposList = await ObtenerEquipos()
            };
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TicketEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.EstadosList = ObtenerEstados();
                model.TiposList = ObtenerTipos();
                model.PrioridadesList = ObtenerPrioridades();
                model.TecnicosList = await ObtenerTecnicos(model.TecnicoAsignadoId);
                model.EquiposList = await ObtenerEquipos();
                return View(model);
            }

            var ticket = await _ticketRepo.GetByIdAsync(model.Id);
            if (ticket == null) return NotFound();

            var prioridadAnterior = ticket.Prioridad;

            ticket.Titulo = model.Titulo;
            ticket.Descripcion = model.Descripcion;
            ticket.Estado = model.Estado;
            ticket.Tipo = model.Tipo;
            ticket.Prioridad = model.Prioridad;
            ticket.TecnicoAsignadoId = model.TecnicoAsignadoId;
            ticket.EquipoId = model.EquipoId;

            if (prioridadAnterior != ticket.Prioridad)
            {
                CalcularYAsignarSLA(ticket);
            }

            await _ticketRepo.UpdateAsync(ticket);
            TempData["Success"] = "Ticket actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Acciones para clientes (calificación y reapertura)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calificar(int id, int calificacion, string? comentario)
        {
            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || ticket.CreadoPorId != userId)
                return Forbid();

            if (ticket.Estado != "Resuelto")
            {
                TempData["Error"] = "Este ticket no está en estado de calificación.";
                return RedirectToAction(nameof(MisTickets));
            }

            ticket.CalificacionCliente = calificacion;
            ticket.ComentarioCliente = comentario;
            ticket.FechaCalificacion = DateTime.Now;
            ticket.Estado = "Cerrado";
            ticket.FechaCierre = DateTime.Now;   // ← Asegurar que se guarda la fecha de cierre

            // Calcular cumplimiento de SLA de resolución
            if (ticket.FechaResolucionEsperada.HasValue)
            {
                ticket.SLA_CumplidoResolucion = ticket.FechaCierre <= ticket.FechaResolucionEsperada;
            }

            await _ticketRepo.UpdateAsync(ticket);

            TempData["Success"] = "Gracias por calificar. El ticket ha sido cerrado.";
            return RedirectToAction(nameof(MisTickets));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reabrir(int id, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["Error"] = "Debe indicar el motivo de la reapertura.";
                return RedirectToAction(nameof(MisTickets));
            }

            var ticket = await _ticketRepo.GetByIdAsync(id);
            if (ticket == null) return NotFound();

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || ticket.CreadoPorId != userId)
                return Forbid();

            if (ticket.Estado != "Resuelto")
            {
                TempData["Error"] = "Solo se pueden reabrir tickets resueltos.";
                return RedirectToAction(nameof(MisTickets));
            }

            ticket.Estado = "Asignado";
            ticket.Reabierto = true;
            ticket.MotivoReapertura = motivo;
            ticket.FechaReapertura = DateTime.Now;
            ticket.ReabiertoPorId = userId;

            ticket.CalificacionCliente = null;
            ticket.ComentarioCliente = null;
            ticket.FechaCalificacion = null;

            await _ticketRepo.UpdateAsync(ticket);

            TempData["Success"] = "Ticket reabierto. El técnico será notificado.";
            return RedirectToAction(nameof(MisTickets));
        }

        #endregion
    }
}