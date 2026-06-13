using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;


namespace OC.Web.Controllers
{

    [Authorize(Roles = "Admin")]
    public class SucursalesController : Controller
    {
        private readonly IGenericRepository<Sucursal> _repository;

        public SucursalesController(IGenericRepository<Sucursal> repository)
        {
            _repository = repository;
        }

        // GET: Sucursales
        public async Task<IActionResult> Index(int page = 1)
        {
            var result = await _repository.GetPagedAsync(
                page,
                10,
                s => s.Activo
            );

            return View(result);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SucursalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Escenario 2
            }

            var entity = new Sucursal
            {
                Nombre = model.Nombre,
                Direccion = model.Direccion,
                Telefono = model.Telefono,
                Activo = true
            };

            await _repository.AddAsync(entity);

            TempData["Success"] = "Sucursal creada exitosamente";
            return RedirectToAction(nameof(Index)); // Escenario 1
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var model = new SucursalViewModel
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Direccion = entity.Direccion,
                Telefono = entity.Telefono
            };

            return View(model);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SucursalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entity = await _repository.GetByIdAsync(model.Id);
            if (entity == null) return NotFound();

            entity.Nombre = model.Nombre;
            entity.Direccion = model.Direccion;
            entity.Telefono = model.Telefono;

            await _repository.UpdateAsync(entity);

            TempData["Success"] = "Sucursal actualizada";
            return RedirectToAction(nameof(Index));
        }

        // POST: ToggleStatus (Soft Delete)
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Activo = !entity.Activo;
            await _repository.UpdateAsync(entity);

            return RedirectToAction(nameof(Index));
        }
    }
}