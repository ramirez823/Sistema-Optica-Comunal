using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PedidosController : Controller
    {
        private readonly IGenericRepository<Pedido> _pedidoRepo;
        private readonly IGenericRepository<Proveedor> _proveedorRepo;
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<DetallePedido> _detalleRepo;
        private readonly AppDbContext _context;

        public PedidosController(
            IGenericRepository<Pedido> pedidoRepo,
            IGenericRepository<Proveedor> proveedorRepo,
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<DetallePedido> detalleRepo,
            AppDbContext context)
        {
            _pedidoRepo = pedidoRepo;
            _proveedorRepo = proveedorRepo;
            _productoRepo = productoRepo;
            _detalleRepo = detalleRepo;
            _context = context;
        }

        // GET: Pedidos con filtros server-side
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            int? cantidadMinima = null)
        {
            // Validación: si el rango está invertido, no aplicamos el filtro de fechas
            // (la SQL devolvería 0 filas, lo que confundiría al usuario).
            // Mantenemos los valores en ViewBag para que pueda corregirlos.
            bool fechasInvalidas = fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde > fechaHasta;
            if (fechasInvalidas)
            {
                TempData["Error"] = "La fecha 'desde' no puede ser mayor que la fecha 'hasta'. Corrige el rango para ver resultados.";
            }

            if (cantidadMinima.HasValue && cantidadMinima < 1)
            {
                cantidadMinima = null;
            }

            // Construcción dinámica del filtro en expression tree
            Expression<Func<Pedido, bool>> filter = p => p.Activo;

            if (!fechasInvalidas)
            {
                if (fechaDesde.HasValue)
                {
                    var desde = fechaDesde.Value.Date;
                    filter = And(filter, p => p.FechaPedido >= desde);
                }

                if (fechaHasta.HasValue)
                {
                    // "Día completo": desde la medianoche hasta el final del día.
                    // Filtramos con < (fechaHasta.Date + 1 día) para incluir todo el día seleccionado.
                    var hastaExclusivo = fechaHasta.Value.Date.AddDays(1);
                    filter = And(filter, p => p.FechaPedido < hastaExclusivo);
                }
            }

            if (cantidadMinima.HasValue)
            {
                var min = cantidadMinima.Value;
                // EF Core 8 traduce esto a un subquery:
                //   WHERE (SELECT ISNULL(SUM(d.Cantidad),0) FROM DetallePedidos d WHERE d.PedidoId = p.Id) >= @min
                filter = And(filter, p => p.Detalles.Sum(d => d.Cantidad) >= min);
            }

            var result = await _pedidoRepo.GetPagedAsync(
                page,
                pageSize,
                filter: filter,
                orderBy: q => q.OrderByDescending(p => p.FechaPedido),
                includeProperties: "Proveedor,Detalles.Producto"  // Fix bug Task 2: Detalles requerido para métricas
            );

            // Persistir los filtros para que el form los conserve
            ViewBag.FechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = fechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.CantidadMinima = cantidadMinima;
            ViewBag.PageSize = pageSize;

            return View(result);
        }

        // GET: Pedidos/Create
        public async Task<IActionResult> Create()
        {
            var model = new PedidoCreateViewModel();
            await CargarListas(model);
            return View(model);
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PedidoCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListas(model);
                return View(model);
            }

            // Validación: al menos una línea de producto
            if (model.Detalles == null || !model.Detalles.Any(d => d.ProductoId > 0 && d.Cantidad > 0))
            {
                ModelState.AddModelError("Detalles", "Debe agregar al menos un producto con cantidad mayor a 0.");
                await CargarListas(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = new Pedido
                {
                    ProveedorId = model.ProveedorId,
                    FechaPedido = DateTime.Now,
                    FechaEntregaEstimada = model.FechaEntregaEstimada,
                    Descripcion = model.Descripcion ?? "",
                    Estado = EstadoPedido.Pendiente,
                    Activo = true
                };

                await _pedidoRepo.AddAsync(pedido);

                foreach (var detalleVm in model.Detalles.Where(d => d.ProductoId > 0 && d.Cantidad > 0))
                {
                    var producto = await _productoRepo.GetByIdAsync(detalleVm.ProductoId);
                    if (producto == null) continue;

                    var detalle = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = detalleVm.ProductoId,
                        Cantidad = detalleVm.Cantidad,
                        CostoUnitario = producto.CostoUnitario
                    };
                    await _detalleRepo.AddAsync(detalle);
                }

                await transaction.CommitAsync();
                TempData["Success"] = "Pedido creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Ocurrió un error al crear el pedido. Intente nuevamente.");
                await CargarListas(model);
                return View(model);
            }
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var pedidoEntity = (await _pedidoRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id && p.Activo,
                includeProperties: "Proveedor,Detalles.Producto"
            )).Items.FirstOrDefault();

            if (pedidoEntity == null) return NotFound();

            var model = new PedidoEditViewModel
            {
                Id = pedidoEntity.Id,
                ProveedorId = pedidoEntity.ProveedorId,
                FechaEntregaEstimada = pedidoEntity.FechaEntregaEstimada,
                Descripcion = pedidoEntity.Descripcion,
                Estado = pedidoEntity.Estado,
                Detalles = pedidoEntity.Detalles?.Select(d => new DetallePedidoViewModel
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    NombreProducto = d.Producto?.Nombre,
                    CostoUnitario = d.CostoUnitario
                }).ToList() ?? new List<DetallePedidoViewModel>()
            };

            await CargarListas(model);
            return View(model);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PedidoEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListas(model);
                return View(model);
            }

            if (model.Detalles == null || !model.Detalles.Any(d => d.ProductoId > 0 && d.Cantidad > 0))
            {
                ModelState.AddModelError("Detalles", "Debe mantener al menos un producto con cantidad mayor a 0.");
                await CargarListas(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Detalles)
                    .FirstOrDefaultAsync(p => p.Id == model.Id && p.Activo);

                if (pedido == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                var estadoAnterior = pedido.Estado;

                pedido.ProveedorId = model.ProveedorId;
                pedido.FechaEntregaEstimada = model.FechaEntregaEstimada;
                pedido.Descripcion = model.Descripcion ?? "";
                pedido.CambiarEstado(model.Estado);

                if (pedido.Detalles != null && pedido.Detalles.Any())
                {
                    _context.DetallePedidos.RemoveRange(pedido.Detalles);
                }

                foreach (var detalleVm in model.Detalles.Where(d => d.ProductoId > 0 && d.Cantidad > 0))
                {
                    var producto = await _productoRepo.GetByIdAsync(detalleVm.ProductoId);
                    if (producto == null) continue;

                    var detalle = new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = detalleVm.ProductoId,
                        Cantidad = detalleVm.Cantidad,
                        CostoUnitario = producto.CostoUnitario
                    };
                    _context.DetallePedidos.Add(detalle);
                }

                if (estadoAnterior != EstadoPedido.Recibido && pedido.Estado == EstadoPedido.Recibido)
                {
                    foreach (var detalleVm in model.Detalles.Where(d => d.ProductoId > 0 && d.Cantidad > 0))
                    {
                        var producto = await _productoRepo.GetByIdAsync(detalleVm.ProductoId);
                        if (producto != null)
                        {
                            producto.Stock += detalleVm.Cantidad;
                            _context.Productos.Update(producto);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Pedido actualizado correctamente.";
                return RedirectToAction(nameof(Details), new { id = pedido.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "El pedido fue modificado o eliminado por otro usuario. Por favor, recargue la página.");
                await CargarListas(model);
                return View(model);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Ocurrió un error al actualizar el pedido. Intente nuevamente.");
                await CargarListas(model);
                return View(model);
            }
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var pedidoEntity = (await _pedidoRepo.GetPagedAsync(
                1, 1,
                filter: p => p.Id == id && p.Activo,
                includeProperties: "Proveedor,Detalles.Producto"
            )).Items.FirstOrDefault();

            if (pedidoEntity == null) return NotFound();
            return View(pedidoEntity);
        }

        // POST: Pedidos/Desactivar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            var pedido = await _pedidoRepo.GetByIdAsync(id);
            if (pedido == null) return NotFound();

            pedido.Activo = false;
            await _pedidoRepo.UpdateAsync(pedido);

            TempData["Success"] = "Pedido desactivado.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Carga los combos de proveedores y productos activos para los formularios.
        /// Funciona tanto para PedidoCreateViewModel como PedidoEditViewModel.
        /// </summary>
        /// <summary>
        /// Combina dos expresiones de predicado en una sola usando AndAlso.
        /// Usa Invoke para que EF Core traduzca ambos lados a SQL.
        /// </summary>
        private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            var param = left.Parameters[0];
            var body = Expression.AndAlso(
                left.Body,
                Expression.Invoke(right, param)
            );
            return Expression.Lambda<Func<T, bool>>(body, param);
        }

        private async Task CargarListas(object model)
        {
            var productosActivos = (await _productoRepo.GetPagedAsync(1, 100, filter: p => p.Activo)).Items.ToList();
            ViewBag.ProductoImagenesJson = JsonSerializer.Serialize(
                productosActivos.ToDictionary(p => p.Id.ToString(), p => p.RutaImagen ?? ""));

            var proveedores = (await _proveedorRepo.GetPagedAsync(1, 100, filter: p => p.Activo))
                .Items.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Nombre,
                    Selected = model switch
                    {
                        PedidoCreateViewModel c => c.ProveedorId == p.Id,
                        PedidoEditViewModel e => e.ProveedorId == p.Id,
                        _ => false
                    }
                });

            var productos = productosActivos
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Nombre} (SKU: {p.SKU})"
                });

            switch (model)
            {
                case PedidoCreateViewModel c:
                    c.Proveedores = proveedores;
                    c.Productos = productos;
                    break;
                case PedidoEditViewModel e:
                    e.Proveedores = proveedores;
                    e.Productos = productos;
                    break;
            }
        }
    }
}
