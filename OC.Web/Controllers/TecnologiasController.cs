using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TecnologiasController : Controller
    {
        private readonly IGenericRepository<TecnologiaLente> _repo;

        public TecnologiasController(IGenericRepository<TecnologiaLente> repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var resultado = await _repo.GetPagedAsync(1, 100, null, q => q.OrderBy(t => t.Nombre));
            return View(resultado.Items);
        }

        public IActionResult Create()
        {
            return View(new TecnologiaLente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TecnologiaLente model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                ModelState.AddModelError("Nombre", "El nombre es requerido.");

            var existente = await _repo.GetPagedAsync(1, 1, t => t.Nombre == model.Nombre.Trim());
            if (existente.TotalCount > 0)
                ModelState.AddModelError("Nombre", "Ya existe una tecnología con este nombre.");

            if (!ModelState.IsValid)
                return View(model);

            model.Nombre = model.Nombre.Trim();
            await _repo.AddAsync(model);

            TempData["Success"] = "Tecnología de lente registrada correctamente.";
            return RedirectToAction("Index", "Inventory", new { seccion = "tecnologias" });
        }

        public async Task<IActionResult> Details(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TecnologiaLente model)
        {
            var existente = await _repo.GetByIdAsync(id);
            if (existente == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Nombre))
                ModelState.AddModelError("Nombre", "El nombre es requerido.");

            var duplicado = await _repo.GetPagedAsync(1, 1,
                t => t.Nombre == model.Nombre.Trim() && t.Id != id);
            if (duplicado.TotalCount > 0)
                ModelState.AddModelError("Nombre", "Ya existe una tecnología con este nombre.");

            if (!ModelState.IsValid)
            {
                existente.Nombre = model.Nombre;
                existente.Precio = model.Precio;
                return View(existente);
            }

            existente.Nombre = model.Nombre.Trim();
            existente.Precio = model.Precio;
            await _repo.UpdateAsync(existente);

            TempData["Success"] = "Tecnología actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();

            await _repo.DeleteAsync(id);
            TempData["Success"] = "Tecnología eliminada.";
            return RedirectToAction("Index", "Inventory", new { seccion = "tecnologias" });
        }
    }
}