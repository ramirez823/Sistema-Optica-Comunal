using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Services;
using OC.Web.ViewModels;
using System.Linq.Expressions;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Recepcion,Optometrista,TecnicoOcular")]
    public class OrdenesTrabajoController : Controller
    {
        private readonly IGenericRepository<OrdenTrabajo> _ordenesRepo;
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Sucursal> _sucursalesRepo;
        private readonly IGenericRepository<Venta> _ventasRepo;
        private readonly INotificationService _notificationService;

        public OrdenesTrabajoController(
            IGenericRepository<OrdenTrabajo> ordenesRepo,
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Sucursal> sucursalesRepo,
            IGenericRepository<Venta> ventasRepo,
            INotificationService notificationService)
        {
            _ordenesRepo = ordenesRepo;
            _pacientesRepo = pacientesRepo;
            _sucursalesRepo = sucursalesRepo;
            _ventasRepo = ventasRepo;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            string? search = null,
            string? sort = null,
            string? estado = null)
        {
            // Default: descending (newest first)
            Func<IQueryable<OrdenTrabajo>, IOrderedQueryable<OrdenTrabajo>> orderBy = q => q.OrderByDescending(o => o.FechaCreacion);

            if (sort == "asc")
                orderBy = q => q.OrderBy(o => o.FechaCreacion);
            else if (sort == "patient")
                orderBy = q => q.OrderBy(o => o.Paciente != null ? o.Paciente.Nombres : "");
            else if (sort == "patient_desc")
                orderBy = q => q.OrderByDescending(o => o.Paciente != null ? o.Paciente.Nombres : "");

            Expression<Func<OrdenTrabajo, bool>>? filter = null;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                int? idBusqueda = null;
                if (int.TryParse(term, out var idNum))
                    idBusqueda = idNum;

                filter = o =>
                    (o.Paciente != null &&
                        (o.Paciente.Nombres.ToLower().Contains(term) ||
                         o.Paciente.Apellidos.ToLower().Contains(term))) ||
                    (o.Referencia != null && o.Referencia.ToLower().Contains(term)) ||
                    o.Estado.ToLower().Contains(term) ||
                    (idBusqueda.HasValue && o.Id == idBusqueda.Value);
            }

            // Filtro por estado (usado por las tarjetas KPI del dashboard).
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estadoFiltro = estado.Trim();
                Expression<Func<OrdenTrabajo, bool>> estadoFilter = o => o.Estado == estadoFiltro;
                filter = filter == null
                    ? estadoFilter
                    : Expression.Lambda<Func<OrdenTrabajo, bool>>(
                        Expression.AndAlso(filter.Body, Expression.Invoke(estadoFilter, filter.Parameters)),
                        filter.Parameters);
            }

            var result = await _ordenesRepo.GetPagedAsync(
                pageIndex: page,
                pageSize: 10,
                filter: filter,
                orderBy: orderBy,
                includeProperties: "Paciente,Sucursal"
            );

            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.Estado = estado;
            return View(result);
        }

        [Authorize(Roles = "Admin,Recepcion,Optometrista")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await RecargarViewBag();
            return View(new OrdenTrabajoViewModel());
        }

        [Authorize(Roles = "Admin,Recepcion,Optometrista")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrdenTrabajoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await RecargarViewBag();
                return View(model);
            }

            var orden = new OrdenTrabajo
            {
                PacienteId = model.PacienteId,
                SucursalId = model.SucursalId,
                VentaId = model.VentaId,
                Referencia = model.Referencia,
                Estado = EstadoOrdenTrabajo.Pendiente,
                FechaCreacion = DateTime.Now,
                PD = model.PD,
                TipoLente = model.TipoLente,
                MaterialLente = model.MaterialLente,
                Tratamientos = model.Tratamientos,
                LaboratorioExterno = model.LaboratorioExterno
            };
            await _ordenesRepo.AddAsync(orden);

            TempData["Success"] = "Orden de trabajo creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var orden = await _ordenesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: o => o.Id == id,
                includeProperties: "Paciente,Sucursal,Venta.ValorClinico"
            );
            var entity = orden.Items.FirstOrDefault();
            if (entity == null) return NotFound();

            ViewBag.Estados = new SelectList(new[]
            {
                new { Value = EstadoOrdenTrabajo.Pendiente, Text = "Pendiente" },
                new { Value = EstadoOrdenTrabajo.EnProceso, Text = "En proceso" },
                new { Value = EstadoOrdenTrabajo.Lista, Text = "Lista" },
                new { Value = EstadoOrdenTrabajo.Entregada, Text = "Entregada" }
            }, "Value", "Text", entity.Estado);

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string estado, decimal? PD, string? TipoLente, string? MaterialLente, string? Tratamientos, string? LaboratorioExterno)
        {
            var ordenResult = await _ordenesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: o => o.Id == id,
                includeProperties: "Paciente,Sucursal,Venta.ValorClinico"
            );
            var orden = ordenResult.Items.FirstOrDefault();
            if (orden == null) return NotFound();

            var estadoAnterior = orden.Estado;

            // Validar transición de estado
            var validTransitions = new Dictionary<(string from, string to), string[]>
            {
                { (EstadoOrdenTrabajo.Pendiente,  EstadoOrdenTrabajo.EnProceso), new[] { "TecnicoOcular", "Admin" } },
                { (EstadoOrdenTrabajo.EnProceso,  EstadoOrdenTrabajo.Lista),     new[] { "TecnicoOcular", "Admin" } },
                { (EstadoOrdenTrabajo.Lista,      EstadoOrdenTrabajo.Entregada), new[] { "Recepcion",    "Admin" } },
            };

            var key = (estadoAnterior, estado);
            if (!validTransitions.TryGetValue(key, out var allowedRoles))
                return BadRequest("Transición de estado inválida.");

            if (!allowedRoles.Any(r => User.IsInRole(r)))
            {
                TempData["Error"] = "No tenés permisos para esta transición.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            orden.Estado = estado ?? orden.Estado;

            // Solo roles NO-Tecnico pueden modificar los campos de fabricación.
            // El Técnico Ocular debe verlos tal como vienen de Optometría / Ventas.
            if (!User.IsInRole("TecnicoOcular"))
            {
                orden.PD = PD;
                orden.TipoLente = TipoLente;
                orden.MaterialLente = MaterialLente;
                orden.Tratamientos = Tratamientos;
                orden.LaboratorioExterno = LaboratorioExterno;
            }

            if (orden.Estado == EstadoOrdenTrabajo.Lista && !orden.FechaLista.HasValue)
                orden.FechaLista = DateTime.Now;

            await _ordenesRepo.UpdateAsync(orden);

            if (orden.Estado == EstadoOrdenTrabajo.Lista && estadoAnterior != EstadoOrdenTrabajo.Lista)
            {
                var enviado = await _notificationService.NotificarLentesListosAsync(orden);
                if (!enviado)
                {
                    TempData["Error"] = "El estado se actualizó a 'Lista', pero no se pudo notificar al paciente: no tiene correo ni teléfono registrado. Registre los datos de contacto del paciente para que reciba la notificación.";
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            TempData["Success"] = "Orden de trabajo actualizada correctamente." + (orden.Estado == EstadoOrdenTrabajo.Lista ? " Se ha notificado al paciente." : "");
            return RedirectToAction(nameof(Index));
        }

        private async Task RecargarViewBag()
        {
            var pacientes = await _pacientesRepo.GetPagedAsync(1, 500, orderBy: q => q.OrderBy(p => p.Nombres));
            var sucursales = await _sucursalesRepo.GetPagedAsync(1, 100, filter: s => s.Activo);

            ViewBag.Pacientes = new SelectList(pacientes.Items.Select(p => new { p.Id, Nombre = p.NombreCompleto + " (" + p.Cedula + ")" }), "Id", "Nombre");
            ViewBag.Sucursales = new SelectList(sucursales.Items, "Id", "Nombre");
        }
    }
}
