using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArosController : Controller
    {
        private readonly IGenericRepository<Aro> _repo;

        public ArosController(IGenericRepository<Aro> repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var resultado = await _repo.GetPagedAsync(1, 100, a => a.Activo, q => q.OrderBy(a => a.Nombre));
            return View(resultado.Items);
        }

        public IActionResult Create()
        {
            return View(new Aro());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Aro model, IFormFile? imagenAro)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre))
                ModelState.AddModelError("Nombre", "El nombre es requerido.");
            if (string.IsNullOrWhiteSpace(model.SKU))
                ModelState.AddModelError("SKU", "El SKU es requerido.");

            var skuNorm = model.SKU.Trim().ToUpperInvariant();
            var existente = await _repo.GetPagedAsync(1, 1, a => a.SKU == skuNorm);
            if (existente.TotalCount > 0)
                ModelState.AddModelError("SKU", "Ya existe un aro con este SKU.");

            if (!ModelState.IsValid)
                return View(model);

            model.SKU = skuNorm;
            model.Activo = true;

            if (imagenAro != null && imagenAro.Length > 0)
            {
                model.RutaImagen = await GuardarImagenAroAsync(imagenAro);
            }

            await _repo.AddAsync(model);

            TempData["Success"] = "Aro registrado correctamente.";
            return RedirectToAction("Index", "Inventory", new { seccion = "aros" });
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
        public async Task<IActionResult> Edit(int id, Aro model, IFormFile? imagenAro)
        {
            var existente = await _repo.GetByIdAsync(id);
            if (existente == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Nombre))
                ModelState.AddModelError("Nombre", "El nombre es requerido.");
            if (string.IsNullOrWhiteSpace(model.SKU))
                ModelState.AddModelError("SKU", "El SKU es requerido.");

            var skuNorm = model.SKU.Trim().ToUpperInvariant();
            var duplicado = await _repo.GetPagedAsync(1, 1,
                a => a.SKU == skuNorm && a.Id != id);
            if (duplicado.TotalCount > 0)
                ModelState.AddModelError("SKU", "Ya existe un aro con este SKU.");

            if (!ModelState.IsValid)
            {
                existente.Nombre = model.Nombre;
                existente.SKU = model.SKU;
                existente.Precio = model.Precio;
                existente.Stock = model.Stock;
                existente.Activo = model.Activo;
                return View(existente);
            }

            existente.Nombre = model.Nombre.Trim();
            existente.SKU = skuNorm;
            existente.Precio = model.Precio;
            existente.Stock = model.Stock;
            existente.Activo = model.Activo;

            if (imagenAro != null && imagenAro.Length > 0)
            {
                TryDeleteImagenFisica(existente.RutaImagen);
                existente.RutaImagen = await GuardarImagenAroAsync(imagenAro);
            }

            await _repo.UpdateAsync(existente);

            TempData["Success"] = "Aro actualizado correctamente.";
            return RedirectToAction("Index", "Inventory", new { seccion = "aros" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();

            item.Activo = false;
            await _repo.UpdateAsync(item);

            TempData["Success"] = "Aro desactivado.";
            return RedirectToAction("Index", "Inventory", new { seccion = "aros" });
        }

        private async Task<string> GuardarImagenAroAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "aros");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
                ext = ".jpg";

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(uploadsFolder, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return "/uploads/aros/" + fileName;
        }

        private void TryDeleteImagenFisica(string? ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return;
            var relative = ruta.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);
            if (System.IO.File.Exists(path))
            {
                try { System.IO.File.Delete(path); } catch { }
            }
        }
    }
}