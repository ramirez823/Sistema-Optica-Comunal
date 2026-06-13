using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Common;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProveedoresController : Controller
    {
        private readonly IGenericRepository<Proveedor> _proveedorRepo;
        private readonly IProveedorRepository _proveedorRepoDedicado;

        public ProveedoresController(
            IGenericRepository<Proveedor> proveedorRepo,
            IProveedorRepository proveedorRepoDedicado)
        {
            _proveedorRepo = proveedorRepo;
            _proveedorRepoDedicado = proveedorRepoDedicado;
        }

        // LISTADO paginado con búsqueda opcional
        public async Task<IActionResult> Index(int page = 1, string? q = null)
        {
            var resultado = await _proveedorRepoDedicado.GetPagedActivosAsync(page, 10, q);

            // Una sola query trae el conteo de pedidos activos para todos los
            // proveedores de esta página. Evita N+1.
            var ids = resultado.Items.Select(p => p.Id).ToList();
            ViewBag.PedidosPorProveedor = await _proveedorRepoDedicado.GetConteoPedidosActivosAsync(ids);

            ViewBag.SearchActual = q;
            return View(resultado);
        }

        // DETALLE de un proveedor
        public async Task<IActionResult> Details(int id)
        {
            var proveedor = await _proveedorRepo.GetByIdAsync(id);
            if (proveedor == null) return NotFound();

            ViewBag.CantidadPedidos = await _proveedorRepoDedicado.ContarPedidosAsync(id);
            return View(proveedor);
        }

        // CREATE GET
        public IActionResult Create() => View();

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Activo = true;
            await _proveedorRepo.AddAsync(model);

            TempData["Success"] = $"Proveedor '{model.Nombre}' registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _proveedorRepo.GetByIdAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedor model)
        {
            if (!ModelState.IsValid) return View(model);

            await _proveedorRepo.UpdateAsync(model);

            TempData["Success"] = $"Proveedor '{model.Nombre}' actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // DESACTIVAR (Soft Delete) con guarda de integridad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var proveedor = await _proveedorRepo.GetByIdAsync(id);
            if (proveedor == null) return NotFound();

            // Si lo vamos a desactivar, validamos que no tenga pedidos activos
            if (proveedor.Activo)
            {
                var tienePedidos = await _proveedorRepoDedicado.TienePedidosActivosAsync(id);
                if (tienePedidos)
                {
                    TempData["Error"] = $"No se puede desactivar '{proveedor.Nombre}' porque tiene pedidos activos asociados. Cancele o complete los pedidos primero.";
                    return RedirectToAction(nameof(Index));
                }
            }

            proveedor.Activo = !proveedor.Activo;
            await _proveedorRepo.UpdateAsync(proveedor);

            TempData["Success"] = proveedor.Activo
                ? $"Proveedor '{proveedor.Nombre}' reactivado."
                : $"Proveedor '{proveedor.Nombre}' desactivado.";

            return RedirectToAction(nameof(Index));
        }
    }
}
