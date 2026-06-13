using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Models;
using System.Linq.Expressions;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<TecnologiaLente> _tecnologiaRepo;
        private readonly IGenericRepository<Aro> _aroRepo;

        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExtensionesImagen = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long TamanoMaxImagenBytes = 5 * 1024 * 1024;

        public InventoryController(
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<TecnologiaLente> tecnologiaRepo,
            IGenericRepository<Aro> aroRepo,

            IWebHostEnvironment env)
        {
            _productoRepo = productoRepo;
            _tecnologiaRepo = tecnologiaRepo;
            _aroRepo = aroRepo;

            _env = env;
        }

        public async Task<IActionResult> Index(
            string seccion = "productos",
            int page = 1, int pageSize = 12,
            string? filtroProducto = null,
            string? filtroTecnologia = null,
            int tecPage = 1, int tecPageSize = 12,
            string? filtroAro = null,
            int aroPage = 1, int aroPageSize = 12)
        {
            // Productos
            // Productos con filtro
            Expression<Func<Producto, bool>> prodFilter = p => p.Activo;
            if (!string.IsNullOrWhiteSpace(filtroProducto))
            {
                var lower = filtroProducto.ToLower();
                prodFilter = p => p.Activo && (p.Nombre.ToLower().Contains(lower) ||
                                                p.SKU.ToLower().Contains(lower));
            }
            var productos = await _productoRepo.GetPagedAsync(page, pageSize, prodFilter, q => q.OrderBy(p => p.Nombre));
            var lowStock = await _productoRepo.GetAllAsync(p => p.Activo && p.Stock < 6);

            // Tecnologias con filtro
            Expression<Func<TecnologiaLente, bool>> tecFilter = t => true;
            if (!string.IsNullOrWhiteSpace(filtroTecnologia))
            {
                var lower = filtroTecnologia.ToLower();
                tecFilter = t => t.Nombre.ToLower().Contains(lower) ||
                                 t.Precio.ToString().Contains(lower);
            }
            var tecnologias = await _tecnologiaRepo.GetPagedAsync(tecPage, tecPageSize, tecFilter, q => q.OrderBy(t => t.Nombre));

            // Aros con filtro
            Expression<Func<Aro, bool>> aroFilter = a => a.Activo;
            if (!string.IsNullOrWhiteSpace(filtroAro))
            {
                var lower = filtroAro.ToLower();
                aroFilter = a => a.Activo && (a.Nombre.ToLower().Contains(lower) ||
                                               a.SKU.ToLower().Contains(lower) ||
                                               a.Precio.ToString().Contains(lower) ||
                                               a.Stock.ToString().Contains(lower));
            }
            var aros = await _aroRepo.GetPagedAsync(aroPage, aroPageSize, aroFilter, q => q.OrderBy(a => a.Nombre));

            ViewBag.Seccion = seccion;
            ViewBag.PageSize = pageSize;
            ViewBag.LowStock = lowStock;
            ViewBag.FiltroProducto = filtroProducto;
            ViewBag.FiltroTecnologia = filtroTecnologia;
            ViewBag.FiltroAro = filtroAro;

            ViewBag.PaginationProductos = new PaginationInfo
            {
                CurrentPage = productos.PageIndex,
                TotalPages = productos.TotalPages,
                GetPageUrl = p => Url.Action("Index", new { seccion, page = p, pageSize, filtroProducto, filtroTecnologia, tecPage, tecPageSize, filtroAro, aroPage, aroPageSize })
            };
            ViewBag.PaginationTecnologias = new PaginationInfo
            {
                CurrentPage = tecnologias.PageIndex,
                TotalPages = tecnologias.TotalPages,
                GetPageUrl = p => Url.Action("Index", new { seccion, page, pageSize, filtroProducto, filtroTecnologia = filtroTecnologia, tecPage = p, tecPageSize, filtroAro, aroPage, aroPageSize })
            };
            ViewBag.PaginationAros = new PaginationInfo
            {
                CurrentPage = aros.PageIndex,
                TotalPages = aros.TotalPages,
                GetPageUrl = p => Url.Action("Index", new { seccion, page, pageSize, filtroProducto, filtroTecnologia, tecPage, tecPageSize, filtroAro = filtroAro, aroPage = p, aroPageSize })
            };
            ViewBag.Tecnologias = tecnologias.Items.ToList();
            ViewBag.Aros = aros.Items.ToList();

            return View(productos);
        }

        public IActionResult Create(string seccion = "productos")
        {
            ViewBag.Seccion = seccion;
// categoria eliminada

            return View(new Producto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                nameof(Producto.Nombre),
                nameof(Producto.SKU),
                nameof(Producto.Categoria),
                nameof(Producto.PrecioPublico),
                nameof(Producto.DescripcionCorta),
                nameof(Producto.CostoUnitario),
                nameof(Producto.Stock),
                nameof(Producto.Destacado))]
            Producto model,
            IFormFile? imagenProducto)
        {
            imagenProducto ??= Request.Form.Files.GetFile("imagenProducto");

            if (string.IsNullOrWhiteSpace(model.SKU))
            {
                ModelState.AddModelError(nameof(Producto.SKU), "El SKU es requerido.");
            }

            var skuNorm = (model.SKU ?? "").Trim().ToUpperInvariant();
            var existente = await _productoRepo.GetPagedAsync(1, 1, p => p.SKU == skuNorm);
            if (existente.TotalCount > 0)
            {
                ModelState.AddModelError(nameof(Producto.SKU), "Ya existe un producto con este SKU.");
            }

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                var err = ValidarImagen(imagenProducto);
                if (err != null)
                    ModelState.AddModelError(nameof(imagenProducto), err);
            }

            if (!ModelState.IsValid)
                return View(model);

            model.SKU = skuNorm;
            model.Activo = true;
            model.RutaImagen = null;

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                model.RutaImagen = await GuardarImagenProductoAsync(imagenProducto);
            }

            await _productoRepo.AddAsync(model);

            TempData["Success"] = "Producto registrado correctamente.";
            return RedirectToAction(nameof(Index), new { seccion = "productos" });
        }

        public async Task<IActionResult> Details(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        public async Task<IActionResult> Edit(int id, string seccion = "productos")
        {
            ViewBag.Seccion = seccion;
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind(
                nameof(Producto.Id),
                nameof(Producto.Nombre),
                nameof(Producto.SKU),
                nameof(Producto.Categoria),
                nameof(Producto.PrecioPublico),
                nameof(Producto.DescripcionCorta),
                nameof(Producto.CostoUnitario),
                nameof(Producto.Stock),
                nameof(Producto.Destacado),
                nameof(Producto.Activo))]
            Producto model,
            IFormFile? imagenProducto)
        {
            imagenProducto ??= Request.Form.Files.GetFile("imagenProducto");

            var existente = await _productoRepo.GetByIdAsync(model.Id);
            if (existente == null)
                return NotFound();

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                var err = ValidarImagen(imagenProducto);
                if (err != null)
                    ModelState.AddModelError(nameof(imagenProducto), err);
            }

            if (!ModelState.IsValid)
            {
                existente.Nombre = model.Nombre;
                existente.Categoria = model.Categoria;
                existente.PrecioPublico = model.PrecioPublico;
                existente.DescripcionCorta = model.DescripcionCorta;
                existente.CostoUnitario = model.CostoUnitario;
                existente.Stock = model.Stock;
                existente.Destacado = model.Destacado;
                existente.Activo = model.Activo;
                return View(existente);
            }

            existente.Nombre = model.Nombre;
            existente.SKU = string.IsNullOrWhiteSpace(model.SKU)
                ? existente.SKU
                : model.SKU.Trim().ToUpperInvariant();
            existente.Categoria = model.Categoria;
            existente.PrecioPublico = model.PrecioPublico;
            existente.DescripcionCorta = model.DescripcionCorta;
            existente.CostoUnitario = model.CostoUnitario;
            existente.Stock = model.Stock;
            existente.Destacado = model.Destacado;
            existente.Activo = model.Activo;

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                TryDeleteImagenFisica(existente.RutaImagen);
                existente.RutaImagen = await GuardarImagenProductoAsync(imagenProducto);
            }

            await _productoRepo.UpdateAsync(existente);

            TempData["Success"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null)
                return NotFound();

            producto.Activo = false;
            await _productoRepo.UpdateAsync(producto);

            TempData["Success"] = "Producto eliminado del catálogo.";
            return RedirectToAction(nameof(Index), new { seccion = "productos" });
        }

        private string? ValidarImagen(IFormFile file)
        {
            if (file.Length > TamanoMaxImagenBytes)
                return "La imagen no puede superar 5 MB.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesImagen.Contains(ext))
                return "Use JPG, PNG, GIF o WEBP.";
            return null;
        }

        private async Task<string> GuardarImagenProductoAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "", "uploads", "productos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ExtensionesImagen.Contains(ext))
                ext = ".jpg";

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/productos/" + uniqueFileName;
        }

        private void TryDeleteImagenFisica(string? rutaRelativa)
        {
            if (string.IsNullOrEmpty(rutaRelativa) || _env.WebRootPath == null)
                return;
            if (!rutaRelativa.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return;

            var relative = rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(_env.WebRootPath, relative);
            if (System.IO.File.Exists(path))
            {
                try { System.IO.File.Delete(path); } catch { /* ignorar bloqueos */ }
            }
        }
    }
}