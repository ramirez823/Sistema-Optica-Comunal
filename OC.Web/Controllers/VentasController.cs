using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Common;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Core.Domain.Enums;
using OC.Web.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace OC.Web.Controllers
{
    [Authorize]
    public class VentasController : Controller
    {
        private readonly IGenericRepository<Venta> _ventaRepo;
        private readonly IGenericRepository<DetalleVenta> _detalleRepo;
        private readonly IGenericRepository<Paciente> _pacienteRepo;
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<ValorClinico> _valorClinicoRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;
        private readonly IGenericRepository<TecnologiaLente> _tecnologiaRepo;
        private readonly IGenericRepository<Aro> _aroRepo;


        public VentasController(
            IGenericRepository<Venta> ventaRepo,
            IGenericRepository<DetalleVenta> detalleRepo,
            IGenericRepository<Paciente> pacienteRepo,
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<ValorClinico> valorClinicoRepo,
            IGenericRepository<Usuario> usuarioRepo,
            IGenericRepository<TecnologiaLente> tecnologiaRepo,
            IGenericRepository<Aro> aroRepo)
        {
            _ventaRepo = ventaRepo;
            _detalleRepo = detalleRepo;
            _pacienteRepo = pacienteRepo;
            _productoRepo = productoRepo;
            _valorClinicoRepo = valorClinicoRepo;
            _usuarioRepo = usuarioRepo;
            _tecnologiaRepo = tecnologiaRepo;
            _aroRepo = aroRepo;
        }

        // GET: /Ventas
        /*   public async Task<IActionResult> Index()
           {
               try
               {
                   var resultado = await _ventaRepo.GetPagedAsync(
                       1, 100, null,
                       includeProperties: "Paciente,Usuario");

                   return View(resultado.Items);
               }
               catch (Exception ex)
               {
                   TempData["Error"] = $"Error al cargar las ventas: {ex.Message}";
                   return RedirectToAction("Index", "Home");
               }
           }*/

        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                var resultado = await _ventaRepo.GetPagedAsync(
                    page, 10, null,
                    includeProperties: "Paciente,Usuario");

                return View(resultado); // 🔥 AQUÍ está la clave
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar las ventas: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Ventas/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var pacientes = await _pacienteRepo.GetPagedAsync(1, 500);
                var productos = await _productoRepo.GetPagedAsync(1, 1000, p => p.Activo && p.Stock > 0);
                var tecnologias = await _tecnologiaRepo.GetPagedAsync(1, 100);
                var aros = await _aroRepo.GetPagedAsync(1, 500, a => a.Activo);

                ViewBag.Pacientes = pacientes.Items.OrderBy(p => p.Apellidos).ToList();
                ViewBag.Productos = productos.Items.OrderBy(p => p.Nombre).ToList();
                ViewBag.MetodosPago = Enum.GetValues<MetodoPago>();
                ViewBag.Tecnologias = tecnologias.Items.OrderBy(t => t.Nombre).ToList();
                ViewBag.Aros = aros.Items.OrderBy(a => a.Nombre).ToList();
                return View(new VentaCreateViewModel());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        // Helper privado para no repetir carga del ViewBag en errores
        private async Task RecargarViewBag()
        {
            var pacientes = await _pacienteRepo.GetPagedAsync(1, 500);
            var productos = await _productoRepo.GetPagedAsync(1, 1000, p => p.Activo && p.Stock > 0);
            var tecnologias = await _tecnologiaRepo.GetPagedAsync(1, 100);
            var aros = await _aroRepo.GetPagedAsync(1, 500, a => a.Activo);

            ViewBag.Pacientes = pacientes.Items.OrderBy(p => p.Apellidos).ToList();
            ViewBag.Productos = productos.Items.OrderBy(p => p.Nombre).ToList();
            ViewBag.MetodosPago = Enum.GetValues<MetodoPago>();
            ViewBag.Tecnologias = tecnologias.Items.OrderBy(t => t.Nombre).ToList();
            ViewBag.Aros = aros.Items.OrderBy(a => a.Nombre).ToList();
        }

        // AJAX GET: /Ventas/BuscarPaciente?id=5
        [HttpGet]
        public async Task<IActionResult> BuscarPaciente(int id)
        {
            try
            {
                var resultado = await _pacienteRepo.GetPagedAsync(
                    1, 1, p => p.Id == id);
                var paciente = resultado.Items.FirstOrDefault();

                if (paciente == null)
                    return NotFound();

                // Buscar su ValorClinico más reciente: el orden se hace en SQL, no en memoria.
                var vcResultado = await _valorClinicoRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 1,
                    filter: vc => vc.Expediente.Cita.PacienteId == id,
                    orderBy: q => q.OrderByDescending(v => v.FechaRegistro),
                    includeProperties: "Expediente.Cita"
                );
                var vc = vcResultado.Items.FirstOrDefault();

                return Json(new
                {
                    id = paciente.Id,
                    nombreCompleto = paciente.NombreCompleto,
                    cedula = paciente.Cedula,
                    valorClinico = vc == null ? null : new
                    {
                        id = vc.Id,
                        fecha = vc.FechaRegistro.ToString("dd/MM/yyyy"),
                        diagnostico = vc.Diagnostico,
                        esferaOD = vc.EsferaOD,
                        cilindroOD = vc.CilindroOD,
                        ejeOD = vc.EjeOD,
                        esferaOI = vc.EsferaOI,
                        cilindroOI = vc.CilindroOI,
                        ejeOI = vc.EjeOI
                    }
                });
            }
            catch
            {
                return StatusCode(500, "Error al buscar el paciente");
            }
        }

        //// AJAX GET: /Ventas/BuscarProducto?q=lente
        //[HttpGet]
        //public async Task<IActionResult> BuscarProducto(string q)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(q))
        //            return Json(new List<object>());

        //        var resultado = await _productoRepo.GetPagedAsync(
        //            1, 10,
        //            p => p.Activo &&
        //                 (p.Nombre.Contains(q) || p.SKU.Contains(q)));

        //        var productos = resultado.Items.Select(p => new
        //        {
        //            id = p.Id,
        //            nombre = p.Nombre,
        //            sku = p.SKU,
        //            stock = p.Stock,
        //            costoUnitario = p.CostoUnitario
        //        });

        //        return Json(productos);
        //    }
        //    catch
        //    {
        //        return StatusCode(500, "Error al buscar productos");
        //    }
        //}

        // POST: /Ventas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VentaCreateViewModel model, IFormFile? comprobante)
        {
            if (!ModelState.IsValid)
            {
                await RecargarViewBag();
                return View(model);
            }

            try
            {
                // 1. Obtener el usuario logueado desde los claims
                var usuarioIdStr = User.FindFirstValue("UserId");
                if (!int.TryParse(usuarioIdStr, out int usuarioId))
                {
                    TempData["Error"] = "No se pudo identificar al usuario en sesión.";
                    return RedirectToAction("Index");
                }

                // Obtener sucursal del usuario logueado
                var usuarioRes = await _usuarioRepo.GetPagedAsync(1, 1, u => u.Id == usuarioId);
                var usuarioActual = usuarioRes.Items.FirstOrDefault();
                if (usuarioActual == null)
                {
                    TempData["Error"] = "No se encontró el usuario en la base de datos.";
                    return RedirectToAction("Index");
                }


                // 2. Deserializar el carrito
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var detalles = JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(
                    model.DetallesJson, opciones);

                if (detalles == null || !detalles.Any())
                {
                    TempData["Error"] = "El carrito está vacío.";
                    await RecargarViewBag();
                    return View(model);
                }

                // 3. Validar stock para cada producto antes de comprometer
                // Validar stock solo para productos del inventario
                foreach (var item in detalles.Where(d => d.ProductoId.HasValue))
                {
                    var prodRes = await _productoRepo.GetPagedAsync(1, 1, p => p.Id == item.ProductoId!.Value);
                    var producto = prodRes.Items.FirstOrDefault();

                    if (producto == null)
                    {
                        TempData["Error"] = $"Producto con ID {item.ProductoId} no encontrado.";
                        await RecargarViewBag();
                        return View(model);
                    }

                    if (producto.Stock < item.Cantidad)
                    {
                        TempData["Error"] =
                            $"Stock insuficiente para '{producto.Nombre}'. " +
                            $"Disponible: {producto.Stock}, solicitado: {item.Cantidad}.";
                        await RecargarViewBag();
                        return View(model);
                    }
                }

                // 4. Manejar archivo de comprobante si existe
                string? rutaComprobante = null;
                if (comprobante != null && comprobante.Length > 0)
                {
                    // Validar tamaño (10MB max)
                    if (comprobante.Length > 10 * 1024 * 1024)
                    {
                        TempData["Error"] = "El archivo de comprobante no puede exceder 10MB.";
                        await RecargarViewBag();
                        return View(model);
                    }

                    // Validar extensión
                    var extension = Path.GetExtension(comprobante.FileName).ToLowerInvariant();
                    if (!new[] { ".jpg", ".jpeg", ".pdf" }.Contains(extension))
                    {
                        TempData["Error"] = "Solo se permiten archivos .jpg, .jpeg y .pdf.";
                        await RecargarViewBag();
                        return View(model);
                    }

                    // Guardar archivo
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "comprobantes-ventas");
                    if (!Directory.Exists(uploadsDir))
                        Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await comprobante.CopyToAsync(stream);
                    }
                    rutaComprobante = $"/uploads/comprobantes-ventas/{fileName}";
                }

                // 5. Calcular total con descuento porcentual e IVA
                var subtotal = detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
                var montoDescuento = subtotal * (model.Descuento / 100);
                var dopoDescuento = subtotal - montoDescuento;
                var iva = dopoDescuento * 0.13m;
                var totalFinal = dopoDescuento + iva;

                // 6. Crear la Venta
                var venta = new Venta
                {
                    PacienteId = model.PacienteId,
                    UsuarioId = usuarioId,
                    SucursalId = usuarioActual.SucursalId,
                    ValorClinicoId = model.ValorClinicoId,
                    MetodoPago = model.MetodoPago,
                    Descuento = model.Descuento,
                    ReferenciaPago = model.ReferenciaPago,
                    RutaComprobante = rutaComprobante,
                    Notas = model.Notas,
                    FechaVenta = DateTime.Now,
                    Total = totalFinal
                };

                await _ventaRepo.AddAsync(venta);

                // 7. Asignar NumeroFactura ahora que tenemos el Id
                venta.NumeroFactura = $"FAC-{DateTime.Now.Year}-{venta.Id:D6}";
                await _ventaRepo.UpdateAsync(venta);

                // 8. Guardar detalles y decrementar stock
                foreach (var item in detalles)
                {
                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = item.ProductoId,
                        DescripcionSnapshot = item.DescripcionSnapshot,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.PrecioUnitario,
                        Subtotal = item.Cantidad * item.PrecioUnitario
                    };
                    await _detalleRepo.AddAsync(detalle);

                    // Decrementar stock
                    if (item.ProductoId.HasValue)
                    {
                        var prodRes = await _productoRepo.GetPagedAsync(1, 1, p => p.Id == item.ProductoId.Value);
                        var producto = prodRes.Items.First();
                        producto.Stock -= item.Cantidad;
                        await _productoRepo.UpdateAsync(producto);
                    }
                }

                TempData["Success"] = $"Venta {venta.NumeroFactura} registrada correctamente.";
                return RedirectToAction("Factura", new { id = venta.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al registrar la venta: {ex.Message}";
                await RecargarViewBag();
                return View(model);
            }
        }

        // GET: /Ventas/Factura/5
        public async Task<IActionResult> Factura(int id)
        {
            try
            {
                var resultado = await _ventaRepo.GetPagedAsync(
                    1, 1,
                    v => v.Id == id,
                    includeProperties: "Paciente,Usuario,Sucursal,Detalles.Producto");

                var venta = resultado.Items.FirstOrDefault();
                if (venta == null)
                {
                    TempData["Error"] = "Factura no encontrada.";
                    return RedirectToAction("Index");
                }

                ValorClinico? vc = null;
                if (venta.ValorClinicoId.HasValue)
                {
                    var vcRes = await _valorClinicoRepo.GetPagedAsync(
                        1, 1, v => v.Id == venta.ValorClinicoId.Value);
                    vc = vcRes.Items.FirstOrDefault();
                }

                var viewModel = new FacturaViewModel
                {
                    Venta = venta,
                    ValorClinico = vc
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar la factura: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Helper privado para no repetir carga del ViewBag en errores

    }
}