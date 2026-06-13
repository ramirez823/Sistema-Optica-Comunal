using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Core.Domain.Enums;
using OC.Web.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace OC.Web.Controllers
{
    public class LandingController : Controller
    {
        private const decimal TasaIvaCostaRica = 0.13m;
        private static readonly JsonSerializerOptions CarritoJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        private readonly IGenericRepository<Producto> _productoRepo;
        private readonly IGenericRepository<Sucursal> _sucursalRepo;
        private readonly IGenericRepository<Cita> _citaRepo;
        private readonly IGenericRepository<SolicitudCita> _solicitudRepo;
        private readonly IGenericRepository<OrdenTrabajo> _ordenRepo;
        private readonly IGenericRepository<Venta> _ventaRepo;
        private readonly IGenericRepository<DetalleVenta> _detalleVentaRepo;   // ← agregado
        private readonly IGenericRepository<EnvioNotificacion> _notificacionRepo;
        private readonly IGenericRepository<Usuario> _usuarioRepo;
        private readonly IWebHostEnvironment _env;

        public LandingController(
            IGenericRepository<Producto> productoRepo,
            IGenericRepository<Sucursal> sucursalRepo,
            IGenericRepository<Cita> citaRepo,
            IGenericRepository<SolicitudCita> solicitudRepo,
            IGenericRepository<OrdenTrabajo> ordenRepo,
            IGenericRepository<Venta> ventaRepo,
            IGenericRepository<DetalleVenta> detalleVentaRepo,
            IGenericRepository<EnvioNotificacion> notificacionRepo,
            IGenericRepository<Usuario> usuarioRepo,
            IWebHostEnvironment env)
        {
            _productoRepo = productoRepo;
            _sucursalRepo = sucursalRepo;
            _citaRepo = citaRepo;
            _solicitudRepo = solicitudRepo;
            _ordenRepo = ordenRepo;
            _ventaRepo = ventaRepo;
            _detalleVentaRepo = detalleVentaRepo;
            _notificacionRepo = notificacionRepo;
            _usuarioRepo = usuarioRepo;
            _env = env;
        }

        // Redirigir trabajadores que accedan al landing por error
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            if (User.Identity.IsAuthenticated && !User.IsInRole("Paciente"))
            {
                context.Result = RedirectToAction("Index", "Home");
            }
            base.OnActionExecuting(context);
        }

        // ========================= PÁGINAS PÚBLICAS =========================
        [AllowAnonymous]
        [Route("landing")]
        [Route("landing/index")]
        public async Task<IActionResult> Index()
        {
            var productosCarrusel = await _productoRepo.GetPagedAsync(1, 6, filter: p => p.Activo && p.Destacado && !string.IsNullOrEmpty(p.RutaImagen));
            if (!productosCarrusel.Items.Any())
                productosCarrusel = await _productoRepo.GetPagedAsync(1, 6, filter: p => p.Activo && !string.IsNullOrEmpty(p.RutaImagen));
            ViewBag.ProductosCarrusel = productosCarrusel.Items;

            var destacados = await _productoRepo.GetPagedAsync(1, 8, filter: p => p.Destacado && p.Activo);
            ViewBag.Destacados = destacados.Items;
            return View();
        }

        [AllowAnonymous]
        [Route("landing/catalogo")]
        public async Task<IActionResult> Catalogo(string? categoria, string? busqueda, int page = 1)
        {
            var productos = await _productoRepo.GetPagedAsync(
                page, 12,
                filter: p => p.Activo &&
                    (string.IsNullOrEmpty(categoria) || p.Categoria == categoria) &&
                    (string.IsNullOrEmpty(busqueda) || p.Nombre.Contains(busqueda) || p.SKU.Contains(busqueda)),
                orderBy: q => q.OrderBy(p => p.Nombre)
            );
            ViewBag.CategoriaActual = categoria;
            ViewBag.Busqueda = busqueda;
            return View(productos);
        }

        [AllowAnonymous]
        [Route("landing/detalle-producto/{id}")]
        public async Task<IActionResult> DetalleProducto(int id)
        {
            var producto = await _productoRepo.GetByIdAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [AllowAnonymous]
        [Route("landing/sucursales")]
        public async Task<IActionResult> Sucursales()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(1, 100, filter: s => s.Activo);
            return View(sucursales.Items);
        }

        [AllowAnonymous]
        [Route("landing/tecnologias")]
        public IActionResult Tecnologias() => View();

        [AllowAnonymous]
        [Route("landing/contacto")]
        public async Task<IActionResult> Contacto()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(
                1, 200,
                filter: s => s.Activo,
                orderBy: q => q.OrderBy(s => s.Nombre));
            return View(sucursales.Items);
        }

        // ========================= SECCIÓN PARA PACIENTES AUTENTICADOS =========================
        [Authorize(Roles = "Paciente")]
        [Route("landing/mis-citas")]
        public async Task<IActionResult> MisCitas(int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var citas = await _citaRepo.GetPagedAsync(page, 10,
                filter: c => c.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(c => c.FechaHora),
                includeProperties: "Sucursal"
            );
            return View(citas);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/estado-orden")]
        public async Task<IActionResult> EstadoOrden(int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var ordenes = await _ordenRepo.GetPagedAsync(page, 10,
                filter: o => o.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(o => o.FechaCreacion),
                includeProperties: "Sucursal"
            );
            return View(ordenes);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/mis-facturas")]
        public async Task<IActionResult> MisFacturas(DateTime? desde, DateTime? hasta, int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            var fechaDesde = desde?.Date;
            var fechaHasta = hasta?.Date.AddDays(1).AddTicks(-1);

            var ventas = await _ventaRepo.GetPagedAsync(page, 9,
                filter: v => v.PacienteId == pacienteId
                             && (!fechaDesde.HasValue || v.FechaVenta >= fechaDesde.Value)
                             && (!fechaHasta.HasValue || v.FechaVenta <= fechaHasta.Value),
                orderBy: q => q.OrderByDescending(v => v.FechaVenta),
                includeProperties: "Detalles,Sucursal"
            );

            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            return View(ventas);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/notificaciones")]
        public async Task<IActionResult> Notificaciones(int page = 1)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");
            var notificaciones = await _notificacionRepo.GetPagedAsync(page, 10,
                filter: n => n.OrdenTrabajo.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(n => n.FechaHoraEnvio),
                includeProperties: "OrdenTrabajo"
            );
            return View(notificaciones);
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/agendar-cita")]
        public async Task<IActionResult> AgendarCita()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(1, 100, filter: s => s.Activo);
            ViewBag.SucursalesList = sucursales.Items.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Nombre }).ToList();
            ViewBag.ServiciosList = new[]
            {
                new SelectListItem { Value = "Examen visual", Text = "Examen visual" },
                new SelectListItem { Value = "Control de lentes", Text = "Control de lentes" },
                new SelectListItem { Value = "Adaptación de lentes de contacto", Text = "Adaptación de lentes de contacto" },
                new SelectListItem { Value = "Consulta general", Text = "Consulta general" }
            };
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/agendar-cita")]
        public async Task<IActionResult> AgendarCita(int sucursalId, string fecha, string hora, string servicio, string? motivo)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            if (sucursalId <= 0 || string.IsNullOrEmpty(fecha) || string.IsNullOrEmpty(hora) || string.IsNullOrEmpty(servicio))
            {
                TempData["Error"] = "Complete todos los campos obligatorios.";
                return RedirectToAction(nameof(AgendarCita));
            }

            if (!DateTime.TryParse(fecha, out var fechaDate))
            {
                TempData["Error"] = "Fecha no válida.";
                return RedirectToAction(nameof(AgendarCita));
            }

            if (!TimeSpan.TryParse(hora, out var horaTime))
            {
                TempData["Error"] = "Hora no válida.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var fechaHora = fechaDate.Date.Add(horaTime);
            if (fechaHora <= DateTime.Now)
            {
                TempData["Error"] = "La fecha y hora deben ser posteriores a la actual.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var ocupado = await _citaRepo.GetPagedAsync(1, 1,
                filter: c => c.SucursalId == sucursalId && c.FechaHora == fechaHora && c.Estado != EstadoCita.Cancelada);
            if (ocupado.Items.Any())
            {
                TempData["Error"] = "El horario seleccionado ya no está disponible.";
                return RedirectToAction(nameof(AgendarCita));
            }

            var solicitud = new SolicitudCita
            {
                PacienteId = pacienteId.Value,
                Motivo = $"{servicio}. {motivo}",
                FechaSolicitud = DateTime.Now,
                Estado = "Aprobada"
            };
            await _solicitudRepo.AddAsync(solicitud);

            var cita = new Cita
            {
                PacienteId = pacienteId.Value,
                SolicitudCitaId = solicitud.Id,
                SucursalId = sucursalId,
                FechaHora = fechaHora,
                MotivoConsulta = $"{servicio}. {motivo}",
                Estado = EstadoCita.Confirmada,
                FechaCreacion = DateTime.Now
            };
            await _citaRepo.AddAsync(cita);

            TempData["Success"] = "Cita agendada correctamente.";
            return RedirectToAction(nameof(MisCitas));
        }

        [HttpGet]
        [Authorize(Roles = "Paciente")]
        [Route("landing/obtener-horas-disponibles")]
        public async Task<IActionResult> ObtenerHorasDisponibles(int sucursalId, string fecha)
        {
            if (sucursalId <= 0 || string.IsNullOrEmpty(fecha))
                return Json(Array.Empty<string>());

            if (!DateTime.TryParse(fecha, out var fechaDate))
                return Json(Array.Empty<string>());

            var horaInicio = 8;
            var horaFin = 18;
            var inicioDia = fechaDate.Date.AddHours(horaInicio);
            var finDia = fechaDate.Date.AddHours(horaFin);

            var citasOcupadas = await _citaRepo.GetPagedAsync(1, 100,
                filter: c => c.SucursalId == sucursalId
                    && c.FechaHora >= inicioDia
                    && c.FechaHora < finDia
                    && c.Estado != EstadoCita.Cancelada
            );

            var slotsOcupados = citasOcupadas.Items
                .Select(c => c.FechaHora.ToString("HH:mm"))
                .ToHashSet();

            var disponibles = new List<string>();
            for (int h = horaInicio; h < horaFin; h++)
            {
                if (!slotsOcupados.Contains($"{h:D2}:00")) disponibles.Add($"{h:D2}:00");
                if (!slotsOcupados.Contains($"{h:D2}:30")) disponibles.Add($"{h:D2}:30");
            }
            return Json(disponibles);
        }

        // ========================= CARRITO DE COMPRAS =========================
        [Authorize(Roles = "Paciente")]
        [Route("landing/carrito")]
        public async Task<IActionResult> Carrito()
        {
            var sucursales = await _sucursalRepo.GetPagedAsync(
                1, 200,
                filter: s => s.Activo,
                orderBy: q => q.OrderBy(s => s.Nombre));
            ViewBag.Sucursales = sucursales.Items;

            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            var carrito = string.IsNullOrEmpty(carritoJson)
                ? new List<DetalleVentaInputModel>()
                : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson, CarritoJsonOptions) ?? new List<DetalleVentaInputModel>();
            return View(carrito);
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/agregar-al-carrito")]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var producto = await _productoRepo.GetByIdAsync(productoId);
            if (producto == null || producto.Stock < cantidad)
            {
                TempData["Error"] = "Producto no disponible o stock insuficiente.";
                return RedirectToAction(nameof(DetalleProducto), new { id = productoId });
            }

            var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
            var carrito = string.IsNullOrEmpty(carritoJson)
                ? new List<DetalleVentaInputModel>()
                : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson, CarritoJsonOptions) ?? new List<DetalleVentaInputModel>();

            var itemExistente = carrito.FirstOrDefault(x => x.ProductoId == productoId);
            if (itemExistente != null)
            {
                if (itemExistente.Cantidad + cantidad > producto.Stock)
                {
                    TempData["Error"] = "Stock insuficiente para la cantidad solicitada.";
                    return RedirectToAction(nameof(DetalleProducto), new { id = productoId });
                }
                itemExistente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new DetalleVentaInputModel
                {
                    ProductoId = productoId,
                    DescripcionSnapshot = producto.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.PrecioPublico
                });
            }

            HttpContext.Session.SetString("CarritoPaciente", JsonSerializer.Serialize(carrito, CarritoJsonOptions));
            TempData["Success"] = "Producto agregado al carrito.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [Route("landing/actualizar-carrito")]
        public IActionResult ActualizarCarrito([FromBody] List<DetalleVentaInputModel>? detalles)
        {
            detalles ??= new List<DetalleVentaInputModel>();
            HttpContext.Session.SetString("CarritoPaciente", JsonSerializer.Serialize(detalles, CarritoJsonOptions));
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Paciente")]
        [ValidateAntiForgeryToken]
        [Route("landing/finalizar-compra")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> FinalizarCompra(
            [FromForm] int metodoPago,
            [FromForm] int sucursalId,
            [FromForm] string? notas,
            [FromForm] List<DetalleVentaInputModel>? detalles,
            IFormFile? comprobantePago)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            if (metodoPago != (int)MetodoPago.Efectivo && metodoPago != (int)MetodoPago.SINPE)
            {
                TempData["Error"] = "En la tienda en línea solo se acepta efectivo o SINPE Móvil.";
                return RedirectToAction(nameof(Carrito));
            }

            if (metodoPago == (int)MetodoPago.SINPE && (comprobantePago == null || comprobantePago.Length == 0))
            {
                TempData["Error"] = "Con SINPE Móvil debe adjuntar un comprobante (imagen o PDF).";
                return RedirectToAction(nameof(Carrito));
            }

            if (sucursalId <= 0)
            {
                TempData["Error"] = "Debe seleccionar una sucursal.";
                return RedirectToAction(nameof(Carrito));
            }

            var sucursal = await _sucursalRepo.GetByIdAsync(sucursalId);
            if (sucursal == null || !sucursal.Activo)
            {
                TempData["Error"] = "Seleccione una sucursal válida.";
                return RedirectToAction(nameof(Carrito));
            }

            if (detalles == null || !detalles.Any())
            {
                var carritoJson = HttpContext.Session.GetString("CarritoPaciente");
                detalles = string.IsNullOrEmpty(carritoJson)
                    ? new List<DetalleVentaInputModel>()
                    : JsonSerializer.Deserialize<List<DetalleVentaInputModel>>(carritoJson, CarritoJsonOptions) ?? new List<DetalleVentaInputModel>();
            }

            detalles = detalles
                .Where(d => d.ProductoId.HasValue && d.Cantidad > 0 && d.PrecioUnitario >= 0)
                .ToList();

            if (!detalles.Any())
            {
                TempData["Error"] = "El carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            foreach (var item in detalles.Where(x => x.ProductoId.HasValue))
            {
                var producto = await _productoRepo.GetByIdAsync(item.ProductoId!.Value);
                if (producto == null || producto.Stock < item.Cantidad)
                {
                    TempData["Error"] = $"Stock insuficiente para {item.DescripcionSnapshot}";
                    return RedirectToAction(nameof(Carrito));
                }
            }

            var adminUser = (await _usuarioRepo.GetPagedAsync(1, 1, filter: u => u.Rol.Nombre == "Admin", includeProperties: "Rol")).Items.FirstOrDefault();
            if (adminUser == null)
                throw new InvalidOperationException("No hay usuario administrador en el sistema");

            var subtotalBase = detalles.Sum(x => x.Cantidad * x.PrecioUnitario);
            var montoIva = Math.Round(subtotalBase * TasaIvaCostaRica, 2, MidpointRounding.AwayFromZero);
            var totalConIva = subtotalBase + montoIva;

            string? rutaComprobante = null;
            if (comprobantePago != null && comprobantePago.Length > 0)
            {
                var err = ValidarComprobante(comprobantePago);
                if (err != null)
                {
                    TempData["Error"] = err;
                    return RedirectToAction(nameof(Carrito));
                }
                rutaComprobante = await GuardarComprobanteAsync(comprobantePago);
            }

            var notasFinales = string.IsNullOrWhiteSpace(notas) ? "" : notas.Trim();
            if (!string.IsNullOrEmpty(rutaComprobante))
            {
                var lineaComp = $"Comprobante adjunto: {rutaComprobante}";
                notasFinales = string.IsNullOrEmpty(notasFinales) ? lineaComp : $"{notasFinales}\n{lineaComp}";
            }

            var venta = new Venta
            {
                NumeroFactura = GenerarNumeroFactura(),
                PacienteId = pacienteId.Value,
                UsuarioId = adminUser.Id,
                SucursalId = sucursalId,
                MetodoPago = (MetodoPago)metodoPago,
                Notas = string.IsNullOrEmpty(notasFinales) ? null : notasFinales,
                FechaVenta = DateTime.Now,
                Total = totalConIva
            };
            await _ventaRepo.AddAsync(venta);

            foreach (var item in detalles)
            {
                var detalle = new DetalleVenta
                {
                    VentaId = venta.Id,
                    ProductoId = item.ProductoId,
                    DescripcionSnapshot = item.DescripcionSnapshot ?? "",
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Cantidad * item.PrecioUnitario
                };
                await _detalleVentaRepo.AddAsync(detalle);

                if (item.ProductoId.HasValue)
                {
                    var producto = await _productoRepo.GetByIdAsync(item.ProductoId.Value);
                    producto.Stock -= item.Cantidad;
                    await _productoRepo.UpdateAsync(producto);
                }
            }

            HttpContext.Session.Remove("CarritoPaciente");

            TempData["Success"] = "Compra realizada con éxito. Factura generada.";
            return RedirectToAction(nameof(Factura), new { id = venta.Id });
        }

        private static string? ValidarComprobante(IFormFile file)
        {
            if (file.Length > 10 * 1024 * 1024)
                return "El comprobante no puede superar 10 MB.";
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var ok = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            if (!ok.Contains(ext))
                return "Formato no permitido. Solo JPG, JPEG, PNG, GIF, WEBP o PDF.";
            return null;
        }

        private async Task<string> GuardarComprobanteAsync(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath ?? "", "uploads", "comprobantes-ventas");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var ok = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
            if (!ok.Contains(ext)) ext = ".pdf";

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(stream);

            return "/uploads/comprobantes-ventas/" + name;
        }

        [Authorize(Roles = "Paciente")]
        [Route("landing/factura/{id}")]
        public async Task<IActionResult> Factura(int id)
        {
            var pacienteId = ObtenerPacienteId();
            if (pacienteId == null) return RedirectToAction("Login", "Account");

            var venta = (await _ventaRepo.GetPagedAsync(
                1, 1,
                filter: v => v.Id == id && v.PacienteId == pacienteId.Value,
                includeProperties: "Paciente,Usuario,Sucursal,Detalles.Producto"
            )).Items.FirstOrDefault();

            if (venta == null) return NotFound();

            // Buscar valor clínico asociado (opcional)
            ValorClinico? valorClinico = null; // si la venta tiene ValorClinicoId, cargarlo
            var viewModel = new FacturaViewModel { Venta = venta, ValorClinico = valorClinico };
            return View(viewModel);
        }

        private string GenerarNumeroFactura()
        {
            var año = DateTime.Now.Year;
            var ultimaVenta = _ventaRepo.GetPagedAsync(1, 1, orderBy: q => q.OrderByDescending(v => v.Id)).Result.Items.FirstOrDefault();
            int correlativo = (ultimaVenta?.Id ?? 0) + 1;
            return $"FAC-{año}-{correlativo:D6}";
        }

        private int? ObtenerPacienteId()
        {
            var claim = User.FindFirstValue("PacienteId");
            return int.TryParse(claim, out int id) ? id : null;
        }
    }
}