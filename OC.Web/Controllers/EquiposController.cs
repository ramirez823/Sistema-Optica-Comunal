using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Tecnico")]
    public class EquiposController : Controller
    {
        private readonly IGenericRepository<Equipo> _equipoRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;

        public EquiposController(
            IGenericRepository<Equipo> equipoRepo,
            IGenericRepository<Usuario> usuarioRepo)
        {
            _equipoRepo = equipoRepo;
            _usuarioRepo = usuarioRepo;
        }

        // GET: Equipos
        public async Task<IActionResult> Index(int page = 1, string? search = null)
        {
            var equipos = await _equipoRepo.GetPagedAsync(
                page, 10,
                filter: string.IsNullOrEmpty(search) ? null :
                    e => e.Nombre.Contains(search) || e.Inventario.Contains(search) || e.NumeroSerie.Contains(search),
                orderBy: q => q.OrderBy(e => e.Nombre),
                includeProperties: "UsuarioAsignado"
            );
            ViewBag.Search = search;
            return View(equipos);
        }

        // GET: Equipos/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new EquipoViewModel
            {
                UsuariosList = await ObtenerUsuarios()
            };
            return View(viewModel);
        }

        // POST: Equipos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EquipoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsuariosList = await ObtenerUsuarios();
                return View(model);
            }

            var equipo = new Equipo
            {
                Nombre = model.Nombre,
                Tipo = model.Tipo,
                Marca = model.Marca,
                Modelo = model.Modelo,
                Procesador = model.Procesador,
                RAM = model.RAM,
                Disco = model.Disco,
                SistemaOperativo = model.SistemaOperativo,
                VersionSO = model.VersionSO,
                UsuarioAsignadoId = model.UsuarioAsignadoId,
                NumeroSerie = model.NumeroSerie,
                Inventario = model.Inventario,
                FechaCompra = model.FechaCompra,
                GarantiaMeses = model.GarantiaMeses,
                Observaciones = model.Observaciones,
                Activo = model.Activo
            };

            await _equipoRepo.AddAsync(equipo);
            TempData["Success"] = "Equipo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Equipos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var equipo = await _equipoRepo.GetByIdAsync(id);
            if (equipo == null) return NotFound();

            var viewModel = new EquipoViewModel
            {
                Id = equipo.Id,
                Nombre = equipo.Nombre,
                Tipo = equipo.Tipo,
                Marca = equipo.Marca,
                Modelo = equipo.Modelo,
                Procesador = equipo.Procesador,
                RAM = equipo.RAM,
                Disco = equipo.Disco,
                SistemaOperativo = equipo.SistemaOperativo,
                VersionSO = equipo.VersionSO,
                UsuarioAsignadoId = equipo.UsuarioAsignadoId,
                NumeroSerie = equipo.NumeroSerie,
                Inventario = equipo.Inventario,
                FechaCompra = equipo.FechaCompra,
                GarantiaMeses = equipo.GarantiaMeses,
                Observaciones = equipo.Observaciones,
                Activo = equipo.Activo,
                UsuariosList = await ObtenerUsuarios(equipo.UsuarioAsignadoId)
            };
            return View(viewModel);
        }

        // POST: Equipos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EquipoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UsuariosList = await ObtenerUsuarios(model.UsuarioAsignadoId);
                return View(model);
            }

            var equipo = await _equipoRepo.GetByIdAsync(model.Id);
            if (equipo == null) return NotFound();

            equipo.Nombre = model.Nombre;
            equipo.Tipo = model.Tipo;
            equipo.Marca = model.Marca;
            equipo.Modelo = model.Modelo;
            equipo.Procesador = model.Procesador;
            equipo.RAM = model.RAM;
            equipo.Disco = model.Disco;
            equipo.SistemaOperativo = model.SistemaOperativo;
            equipo.VersionSO = model.VersionSO;
            equipo.UsuarioAsignadoId = model.UsuarioAsignadoId;
            equipo.NumeroSerie = model.NumeroSerie;
            equipo.Inventario = model.Inventario;
            equipo.FechaCompra = model.FechaCompra;
            equipo.GarantiaMeses = model.GarantiaMeses;
            equipo.Observaciones = model.Observaciones;
            equipo.Activo = model.Activo;

            await _equipoRepo.UpdateAsync(equipo);
            TempData["Success"] = "Equipo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Equipos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var equipo = (await _equipoRepo.GetPagedAsync(
                1, 1,
                filter: e => e.Id == id,
                includeProperties: "UsuarioAsignado"
            )).Items.FirstOrDefault();

            if (equipo == null) return NotFound();
            return View(equipo);
        }

        // POST: Equipos/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var equipo = await _equipoRepo.GetByIdAsync(id);
            if (equipo == null) return NotFound();

            equipo.Activo = !equipo.Activo;
            await _equipoRepo.UpdateAsync(equipo);
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerUsuarios(int? seleccionado = null)
        {
            var usuarios = await _usuarioRepo.GetPagedAsync(1, 100, filter: u => u.Activo, orderBy: q => q.OrderBy(u => u.Nombre));
            return usuarios.Items.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.Nombre,
                Selected = (seleccionado.HasValue && u.Id == seleccionado.Value)
            });
        }
    }
}