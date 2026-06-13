using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
 
    public class PlanillasController : Controller
    {
        private readonly IGenericRepository<Planilla> _planillaRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;

        public PlanillasController(
            IGenericRepository<Planilla> planillaRepo,
            IGenericRepository<Usuario> usuarioRepo)
        {
            _planillaRepo = planillaRepo;
            _usuarioRepo = usuarioRepo;
        }

        // GET: Planillas/Index
        public async Task<IActionResult> Index(int page = 1)
        {
            var planillas = await _planillaRepo.GetPagedAsync(
                page, 15,
                orderBy: q => q.OrderByDescending(p => p.Año).ThenByDescending(p => p.Mes),
                includeProperties: "Usuario"
            );
            return View(planillas);
        }


        // GET: Planillas/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var viewModel = new PlanillaCreateViewModel
            {
                Mes = DateTime.Now.ToString("MMMM", new System.Globalization.CultureInfo("es-CR")),
                Año = DateTime.Now.Year,
                EmpleadosList = await ObtenerEmpleadosSelectList(),
                PorcentajeCCSS = 10.83m
            };
            return View(viewModel);
        }

        // POST: Planillas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlanillaCreateViewModel model)
        {
            // Recargar lista de empleados por si hay error
            model.EmpleadosList = await ObtenerEmpleadosSelectList(model.UsuarioId);

            if (!ModelState.IsValid)
                return View(model);

            // Validar que el empleado tenga salario base
            var empleado = await _usuarioRepo.GetByIdAsync(model.UsuarioId);
            if (empleado == null || empleado.SalarioBase == null)
            {
                ModelState.AddModelError("", "El empleado seleccionado no tiene un salario base definido.");
                return View(model);
            }

            // Calcular adelanto neto (50% del salario base con CCSS aplicada)
            var adelantoBruto = empleado.SalarioBase.Value * 0.5m;
            model.AdelantoQuincena = adelantoBruto * (1 - (model.PorcentajeCCSS / 100));

            // Calcular todos los valores
            CalcularPlanilla(model, empleado.SalarioBase.Value);

            // Mapear a entidad (sin número de comprobante aún)
            var planilla = new Planilla
            {
                UsuarioId = model.UsuarioId,
                Mes = model.Mes,
                Año = model.Año,
                HorasBase = model.HorasBase,
                TotalHoras = model.TotalHoras,
                HorasVacaciones = model.HorasVacaciones,
                HorasIncapacidadParcial = model.HorasIncapacidadParcial,
                HorasIncapacidadTotal = model.HorasIncapacidadTotal,
                HorasPermiso = model.HorasPermiso,
                HorasExtras = model.HorasExtras,
                HorasDobles = model.HorasDobles,
                Comisiones = model.Comisiones,
                Prestamos = model.Prestamos,
                EmbargosPensiones = model.EmbargosPensiones,
                CuentasPorCobrar = model.CuentasPorCobrar,
                AdelantoQuincena = model.AdelantoQuincena,
                PorcentajeCCSS = model.PorcentajeCCSS,
                PorcentajeSolidarista = model.PorcentajeSolidarista,
                NumeroComprobante = "", // Temporal
                SalarioOrdinario = model.SalarioOrdinario,
                ValorHorasExtras = model.ValorHorasExtras,
                ValorHorasDobles = model.ValorHorasDobles,
                ValorVacaciones = model.ValorVacaciones,
                ValorIncapacidadParcial = model.ValorIncapacidadParcial,
                ValorIncapacidadTotal = model.ValorIncapacidadTotal,
                MontoCCSS = model.MontoCCSS,
                MontoImpuestoRenta = model.MontoImpuestoRenta,
                MontoSolidarista = model.MontoSolidarista,
                TotalIngresos = model.TotalIngresos,
                TotalDeducciones = model.TotalDeducciones,
                SalarioNeto = model.SalarioNeto
            };

            await _planillaRepo.AddAsync(planilla);

            // Generar número de comprobante basado en el Id
            planilla.NumeroComprobante = $"PLA-{planilla.Id:D6}";
            await _planillaRepo.UpdateAsync(planilla);

            TempData["Success"] = "Planilla calculada y guardada correctamente.";
            return RedirectToAction(nameof(Comprobante), new { id = planilla.Id });
        }

        // GET: Planillas/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var planilla = (await _planillaRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id,
                includeProperties: "Usuario"
            )).Items.FirstOrDefault();

            if (planilla == null)
                return NotFound();

            var viewModel = new PlanillaCreateViewModel
            {
                Id = planilla.Id,
                UsuarioId = planilla.UsuarioId,
                Mes = planilla.Mes,
                Año = planilla.Año,
                HorasBase = planilla.HorasBase,
                TotalHoras = planilla.TotalHoras,
                HorasVacaciones = planilla.HorasVacaciones,
                HorasIncapacidadParcial = planilla.HorasIncapacidadParcial,
                HorasIncapacidadTotal = planilla.HorasIncapacidadTotal,
                HorasPermiso = planilla.HorasPermiso,
                HorasExtras = planilla.HorasExtras,
                HorasDobles = planilla.HorasDobles,
                Comisiones = planilla.Comisiones,
                Prestamos = planilla.Prestamos,
                EmbargosPensiones = planilla.EmbargosPensiones,
                CuentasPorCobrar = planilla.CuentasPorCobrar,
                AdelantoQuincena = planilla.AdelantoQuincena,
                PorcentajeCCSS = planilla.PorcentajeCCSS,
                PorcentajeSolidarista = planilla.PorcentajeSolidarista,
                NumeroComprobante = planilla.NumeroComprobante,
                EmpleadosList = await ObtenerEmpleadosSelectList(planilla.UsuarioId)
            };

            // Cargar datos del empleado para mostrar en la vista
            var empleado = await _usuarioRepo.GetByIdAsync(planilla.UsuarioId);
            if (empleado != null)
            {
                viewModel.NombreEmpleado = empleado.Nombre;
                viewModel.Cedula = empleado.Cedula;
                viewModel.SalarioBase = empleado.SalarioBase;
            }

            // Pasar los calculados
            viewModel.SalarioOrdinario = planilla.SalarioOrdinario;
            viewModel.ValorHorasExtras = planilla.ValorHorasExtras;
            viewModel.ValorHorasDobles = planilla.ValorHorasDobles;
            viewModel.ValorVacaciones = planilla.ValorVacaciones;
            viewModel.ValorIncapacidadParcial = planilla.ValorIncapacidadParcial;
            viewModel.ValorIncapacidadTotal = planilla.ValorIncapacidadTotal;
            viewModel.MontoCCSS = planilla.MontoCCSS;
            viewModel.MontoImpuestoRenta = planilla.MontoImpuestoRenta;
            viewModel.MontoSolidarista = planilla.MontoSolidarista;
            viewModel.TotalIngresos = planilla.TotalIngresos;
            viewModel.TotalDeducciones = planilla.TotalDeducciones;
            viewModel.SalarioNeto = planilla.SalarioNeto;

            return View(viewModel);
        }

        // POST: Planillas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlanillaCreateViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            model.EmpleadosList = await ObtenerEmpleadosSelectList(model.UsuarioId);

            if (!ModelState.IsValid)
                return View(model);

            var planilla = await _planillaRepo.GetByIdAsync(id);
            if (planilla == null)
                return NotFound();

            var empleado = await _usuarioRepo.GetByIdAsync(model.UsuarioId);
            if (empleado == null || empleado.SalarioBase == null)
            {
                ModelState.AddModelError("", "El empleado seleccionado no tiene un salario base definido.");
                return View(model);
            }

            // Forzar adelanto (por seguridad)
            model.AdelantoQuincena = empleado.SalarioBase.Value * 0.5m;

            // Recalcular
            CalcularPlanilla(model, empleado.SalarioBase.Value);

            // Actualizar entidad
            planilla.UsuarioId = model.UsuarioId;
            planilla.Mes = model.Mes;
            planilla.Año = model.Año;
            planilla.HorasBase = model.HorasBase;
            planilla.TotalHoras = model.TotalHoras;
            planilla.HorasVacaciones = model.HorasVacaciones;
            planilla.HorasIncapacidadParcial = model.HorasIncapacidadParcial;
            planilla.HorasIncapacidadTotal = model.HorasIncapacidadTotal;
            planilla.HorasPermiso = model.HorasPermiso;
            planilla.HorasExtras = model.HorasExtras;
            planilla.HorasDobles = model.HorasDobles;
            planilla.Comisiones = model.Comisiones;
            planilla.Prestamos = model.Prestamos;
            planilla.EmbargosPensiones = model.EmbargosPensiones;
            planilla.CuentasPorCobrar = model.CuentasPorCobrar;
            planilla.AdelantoQuincena = model.AdelantoQuincena;
            planilla.PorcentajeCCSS = model.PorcentajeCCSS;
            planilla.PorcentajeSolidarista = model.PorcentajeSolidarista;
            planilla.NumeroComprobante = model.NumeroComprobante;

            planilla.SalarioOrdinario = model.SalarioOrdinario;
            planilla.ValorHorasExtras = model.ValorHorasExtras;
            planilla.ValorHorasDobles = model.ValorHorasDobles;
            planilla.ValorVacaciones = model.ValorVacaciones;
            planilla.ValorIncapacidadParcial = model.ValorIncapacidadParcial;
            planilla.ValorIncapacidadTotal = model.ValorIncapacidadTotal;
            planilla.MontoCCSS = model.MontoCCSS;
            planilla.MontoImpuestoRenta = model.MontoImpuestoRenta;
            planilla.MontoSolidarista = model.MontoSolidarista;
            planilla.TotalIngresos = model.TotalIngresos;
            planilla.TotalDeducciones = model.TotalDeducciones;
            planilla.SalarioNeto = model.SalarioNeto;

            await _planillaRepo.UpdateAsync(planilla);

            TempData["Success"] = "Planilla actualizada correctamente.";
            return RedirectToAction(nameof(Comprobante), new { id });
        }

        // GET: Planillas/Comprobante/5
        [Authorize(Roles = "Admin,Recepcion,Optometrista,Tecnico")]
        public async Task<IActionResult> Comprobante(int id)
        {
            var planilla = (await _planillaRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id,
                includeProperties: "Usuario"
            )).Items.FirstOrDefault();

            if (planilla == null)
                return NotFound();

            // Validar permisos: solo el dueño o admin pueden ver
            if (!User.IsInRole("Admin"))
            {
                var usuarioIdStr = User.FindFirstValue("UserId");
                if (string.IsNullOrEmpty(usuarioIdStr) || !int.TryParse(usuarioIdStr, out int userId) || planilla.UsuarioId != userId)
                {
                    return Forbid();
                }
            }

            // Determinar URL de retorno según el rol
            ViewBag.ReturnUrl = User.IsInRole("Admin")
                ? Url.Action("Index", "Planillas")
                : Url.Action("MisPlanillas", "Planillas");

            return View(planilla);
        }
        // GET: Planillas/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var planilla = (await _planillaRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id,
                includeProperties: "Usuario"
            )).Items.FirstOrDefault();

            if (planilla == null)
                return NotFound();

            return View(planilla);
        }

        // AJAX: Buscar empleados por cédula o nombre
        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            var pacientesRolId = (await _usuarioRepo.GetPagedAsync(1, 1, filter: r => r.Rol.Nombre == "Paciente")).Items.FirstOrDefault()?.RolId;

            var empleados = await _usuarioRepo.GetPagedAsync(
                1, 20,
                filter: u => u.Activo && (u.Nombre.Contains(term) || u.Cedula.Contains(term))
                             && (pacientesRolId == null || u.RolId != pacientesRolId),
                includeProperties: "Rol"
            );

            var results = empleados.Items.Select(u => new
            {
                id = u.Id,
                text = $"{u.Nombre} - Céd: {u.Cedula}",
                salarioBase = u.SalarioBase ?? 0,
                cedula = u.Cedula,
                nombre = u.Nombre
            });

            return Json(results);
        }

        // Métodos auxiliares
        private async Task<IEnumerable<SelectListItem>> ObtenerEmpleadosSelectList(int? seleccionado = null)
        {
            var pacientesRolId = (await _usuarioRepo.GetPagedAsync(1, 1, filter: r => r.Rol.Nombre == "Paciente")).Items.FirstOrDefault()?.RolId;

            var empleados = await _usuarioRepo.GetPagedAsync(
                1, 100,
                filter: u => u.Activo && (pacientesRolId == null || u.RolId != pacientesRolId),
                orderBy: q => q.OrderBy(u => u.Nombre),
                includeProperties: "Rol"
            );

            return empleados.Items.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Nombre} - Céd: {u.Cedula}",
                Selected = (seleccionado.HasValue && u.Id == seleccionado.Value)
            });
        }

        private void CalcularPlanilla(PlanillaCreateViewModel model, decimal salarioBase)
        {
            decimal valorHora = salarioBase / 240;

            // Ingresos
            model.SalarioOrdinario = valorHora * model.HorasBase;
            model.ValorHorasExtras = valorHora * 1.5m * model.HorasExtras;
            model.ValorHorasDobles = valorHora * 2 * model.HorasDobles;
            model.ValorVacaciones = valorHora * model.HorasVacaciones;
            model.ValorIncapacidadParcial = valorHora * model.HorasIncapacidadParcial;
            model.ValorIncapacidadTotal = valorHora * model.HorasIncapacidadTotal;

            // Total ingresos (sin adelanto)
            model.TotalIngresos = model.SalarioOrdinario + model.ValorHorasExtras + model.ValorHorasDobles
                + model.ValorVacaciones + model.ValorIncapacidadParcial + model.ValorIncapacidadTotal
                + model.Comisiones;


            // Deducciones
            model.MontoCCSS = model.TotalIngresos * (model.PorcentajeCCSS / 100);
            model.MontoSolidarista = model.TotalIngresos * (model.PorcentajeSolidarista / 100);

            decimal baseImponible = model.TotalIngresos - model.MontoCCSS;
            model.MontoImpuestoRenta = CalcularImpuestoRenta(baseImponible);

            model.TotalDeducciones = model.MontoCCSS + model.Prestamos + model.EmbargosPensiones
                + model.CuentasPorCobrar + model.MontoSolidarista + model.MontoImpuestoRenta + model.AdelantoQuincena;

            model.SalarioNeto = model.TotalIngresos - model.TotalDeducciones;
        }

        private decimal CalcularImpuestoRenta(decimal baseImponible)
        {
            // Tabla de impuesto sobre la renta 2026
            if (baseImponible <= 918000)
                return 0;
            else if (baseImponible <= 1347000)
                return (baseImponible - 918000) * 0.10m;
            else if (baseImponible <= 2364000)
                return (1347000 - 918000) * 0.10m + (baseImponible - 1347000) * 0.15m;
            else if (baseImponible <= 4727000)
                return (1347000 - 918000) * 0.10m + (2364000 - 1347000) * 0.15m + (baseImponible - 2364000) * 0.20m;
            else
                return (1347000 - 918000) * 0.10m + (2364000 - 1347000) * 0.15m + (4727000 - 2364000) * 0.20m + (baseImponible - 4727000) * 0.25m;
        }

        [Authorize(Roles = "Admin,Recepcion,Optometrista,Tecnico")]
        public async Task<IActionResult> MisPlanillas(int page = 1)
        {
            var usuarioIdStr = User.FindFirstValue("UserId");
            if (!int.TryParse(usuarioIdStr, out int usuarioId))
                return RedirectToAction("Login", "Account");

            var planillas = await _planillaRepo.GetPagedAsync(
                page, 10,
                filter: p => p.UsuarioId == usuarioId,
                orderBy: q => q.OrderByDescending(p => p.Año).ThenByDescending(p => p.Mes),
                includeProperties: "Usuario"
            );

            return View(planillas);
        }
    }
}