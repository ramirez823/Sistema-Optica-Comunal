using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IGenericRepository<OrdenTrabajo> _ordenesRepo;

        public HomeController(
            ILogger<HomeController> logger,
            IGenericRepository<OrdenTrabajo> ordenesRepo)
        {
            _logger = logger;
            _ordenesRepo = ordenesRepo;
        }

        public async Task<IActionResult> Index()
        {
            var rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
            ViewBag.ModulosGroups = GetModulosGroups(rol);

            // Pacientes van a la landing / su dashboard específico.
            if (User.IsInRole("Paciente"))
                return RedirectToAction("Index", "Landing");

            // Cola de trabajo (solo para roles que tienen tareas accionables).
            if (User.IsInRole("TecnicoOcular") || User.IsInRole("Admin") || User.IsInRole("Recepcion"))
            {
                var ordenesPendientesResult = await _ordenesRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 1,
                    filter: o => o.Estado == EstadoOrdenTrabajo.Pendiente
                );
                var ordenesEnProcesoResult = await _ordenesRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 1,
                    filter: o => o.Estado == EstadoOrdenTrabajo.EnProceso
                );
                var ordenesListasResult = await _ordenesRepo.GetPagedAsync(
                    pageIndex: 1,
                    pageSize: 1,
                    filter: o => o.Estado == EstadoOrdenTrabajo.Lista
                );

                ViewBag.OrdenesPendientes = ordenesPendientesResult.TotalCount;
                ViewBag.OrdenesEnProceso = ordenesEnProcesoResult.TotalCount;
                ViewBag.OrdenesListas = ordenesListasResult.TotalCount;
            }

            return View();
        }

        private static List<ModuloGroup> GetModulosGroups(string rol) => rol switch
        {
            "Admin" => new()
            {
                new ModuloGroup
                {
                    Title = "Gestión Clínica",
                    Icon = "bi-hospital",
                    Items = new()
                    {
                        new("bi-person-heart",   "Pacientes",       "Gestión de pacientes",    "/Pacientes",                   "#93c5fd", "rgba(59,130,246,.25)"),
                        new("bi-calendar-event", "Citas",           "Agendar y gestionar",     "/CitasPublicas/CitasPaciente", "#67e8f9", "rgba(6,182,212,.25)"),
                        new("bi-eyeglasses",     "Órdenes Trabajo", "Seguimiento de órdenes",  "/OrdenesTrabajo",              "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Gestión Comercial",
                    Icon = "bi-cash-stack",
                    Items = new()
                    {
                        new("bi-receipt",        "Ventas",          "Registrar cobros",        "/Ventas",                      "#6ee7b7", "rgba(16,185,129,.25)"),
                        new("bi-box-seam",       "Inventario",      "Productos y stock",       "/Inventory",                   "#fcd34d", "rgba(245,158,11,.25)"),
                        new("bi-truck",          "Pedidos",         "Órdenes a proveedores",   "/Pedidos",                     "#fca5a5", "rgba(239,68,68,.25)"),
                        new("bi-building-check", "Proveedores",     "Gestión de proveedores",  "/Proveedores",                 "#cbd5e1", "rgba(148,163,184,.2)"),
                        new("bi-geo-alt",        "Sucursales",      "Gestión de sucursales",   "/Sucursales",                  "#c4b5fd", "rgba(124,58,237,.25)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new()
                    {
                        new("bi-shield-lock",    "Usuarios",        "Cuentas y roles",         "/Usuarios",                    "#fca5a5", "rgba(220,38,38,.25)"),
                        new("bi-calculator",     "Planillas",       "Gestión de planillas",    "/Planillas",                   "#fcd34d", "rgba(245,158,11,.2)"),
                        new("bi-clock",          "Asistencia",      "Registro de asistencia",  "/Asistencia",                  "#93c5fd", "rgba(59,130,246,.2)"),
                        new("bi-clock",          "Permiso",         "Solicitud de permisos",   "/Permiso",                     "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Reportes y Análisis",
                    Icon = "bi-bar-chart",
                    Items = new()
                    {
                        new("bi-bar-chart-line", "Reportes",        "Análisis gerencial",      "/Reportes",                    "#6ee7b7", "rgba(5,150,105,.25)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new()
                    {
                        new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                        new("bi-pc-display",     "Equipos TI",      "Inventario de equipos",   "/Equipos",                     "#94a3b8", "rgba(71,85,105,.3)"),
                    }
                }
            },

            "Recepcion" => new()
            {
                new ModuloGroup
                {
                    Title = "Gestión Clínica",
                    Icon = "bi-hospital",
                    Items = new()
                    {
                        new("bi-person-heart",   "Pacientes",       "Gestión de pacientes",    "/Pacientes",                   "#93c5fd", "rgba(59,130,246,.25)"),
                        new("bi-calendar-event", "Citas",           "Agendar y gestionar",     "/CitasPublicas/CitasPaciente", "#67e8f9", "rgba(6,182,212,.25)"),
                        new("bi-eyeglasses",     "Órdenes Trabajo", "Seguimiento de órdenes",  "/OrdenesTrabajo",              "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Gestión Comercial",
                    Icon = "bi-cash-stack",
                    Items = new()
                    {
                        new("bi-receipt",        "Ventas",          "Registrar cobros",        "/Ventas",                      "#6ee7b7", "rgba(16,185,129,.25)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new()
                    {
                        new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",      "#fcd34d", "rgba(245,158,11,.2)"),
                        new("bi-clock",          "Asistencia",      "Registro de asistencia",  "/Asistencia",                  "#93c5fd", "rgba(59,130,246,.2)"),
                        new("bi-clock",          "Permiso",         "Solicitud de permisos",   "/Permiso",                     "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new()
                    {
                        new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                    }
                }
            },

            "Optometrista" => new()
            {
                new ModuloGroup
                {
                    Title = "Gestión Clínica",
                    Icon = "bi-hospital",
                    Items = new()
                    {
                        new("bi-person-heart",   "Pacientes",       "Gestión de pacientes",    "/Pacientes",                   "#93c5fd", "rgba(59,130,246,.25)"),
                        new("bi-calendar-event", "Citas",           "Agendar y gestionar",     "/CitasPublicas/CitasPaciente", "#67e8f9", "rgba(6,182,212,.25)"),
                        new("bi-eyeglasses",     "Órdenes Trabajo", "Seguimiento de órdenes",  "/OrdenesTrabajo",              "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new()
                    {
                        new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",      "#fcd34d", "rgba(245,158,11,.2)"),
                        new("bi-clock",          "Asistencia",      "Registro de asistencia",  "/Asistencia",                  "#93c5fd", "rgba(59,130,246,.2)"),
                        new("bi-clock",          "Permiso",         "Solicitud de permisos",   "/Permiso",                     "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new()
                    {
                        new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                    }
                }
            },

            // ════════════════════════════════════════════════════════════
            // TÉCNICO OCULAR — fabricación de lentes
            // Sólo ve Órdenes de Trabajo (para cambiar el estado de las
            // que tiene asignadas), más las herramientas de RR.HH. y
            // Mesa de Ayuda que comparte con el resto del personal.
            // ════════════════════════════════════════════════════════════
            "TecnicoOcular" => new()
            {
                new ModuloGroup
                {
                    Title = "Fabricación de Lentes",
                    Icon = "bi-eyeglasses",
                    Items = new()
                    {
                        new("bi-clipboard2-pulse", "Pendientes",          "Órdenes por iniciar",         "/OrdenesTrabajo?estado=" + EstadoOrdenTrabajo.Pendiente,  "#fcd34d", "rgba(245,158,11,.25)"),
                        new("bi-gear",             "En Proceso",          "Órdenes en fabricación",      "/OrdenesTrabajo?estado=" + EstadoOrdenTrabajo.EnProceso, "#67e8f9", "rgba(6,182,212,.25)"),
                        new("bi-check2-circle",    "Listas para Entrega", "Notificadas al paciente",     "/OrdenesTrabajo?estado=" + EstadoOrdenTrabajo.Lista,     "#6ee7b7", "rgba(16,185,129,.25)"),
                        new("bi-list-ul",          "Todas las Órdenes",   "Listado completo",            "/OrdenesTrabajo",                                        "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new()
                    {
                        new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",      "#fcd34d", "rgba(245,158,11,.2)"),
                        new("bi-clock",          "Asistencia",      "Registro de asistencia",  "/Asistencia",                  "#93c5fd", "rgba(59,130,246,.2)"),
                        new("bi-clock",          "Permiso",         "Solicitud de permisos",   "/Permiso",                     "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new()
                    {
                        new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                    }
                }
            },

            "Tecnico" => new()
            {
                new ModuloGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new()
                    {
                        new("bi-file-earmark-text","Mis Planillas", "Comprobantes de pago",    "/Planillas/MisPlanillas",      "#fcd34d", "rgba(245,158,11,.2)"),
                        new("bi-clock",          "Permiso",         "Solicitud de permisos",   "/Permiso",                     "#93c5fd", "rgba(59,130,246,.2)"),
                        new("bi-clock",          "Asistencia",      "Registro de asistencia",  "/Asistencia",                  "#93c5fd", "rgba(59,130,246,.2)"),
                    }
                },
                new ModuloGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new()
                    {
                        new("bi-headset",        "Mesa de Ayuda",   "Tickets de soporte",      "/Tickets/MisTickets",          "#fcd34d", "rgba(217,119,6,.25)"),
                        new("bi-pc-display",     "Equipos TI",      "Inventario de equipos",   "/Equipos",                     "#94a3b8", "rgba(71,85,105,.3)"),
                    }
                }
            },

            "Paciente" => new()
            {
                new ModuloGroup
                {
                    Title = "Mi cuenta",
                    Icon = "bi-person",
                    Items = new()
                    {
                        new("bi-calendar-event", "Mis Citas",       "Ver y gestionar citas",   "/PacienteDashboard",                "#93c5fd", "rgba(59,130,246,.25)"),
                        new("bi-calendar-plus",  "Solicitar Cita",  "Nueva solicitud",         "/PacienteDashboard/AgendarCita",    "#67e8f9", "rgba(6,182,212,.25)"),
                        new("bi-eyeglasses",     "Estado de Orden", "Ver mis lentes",          "/PacienteDashboard/EstadoOrden",    "#c4b5fd", "rgba(139,92,246,.25)"),
                        new("bi-receipt",        "Mis Facturas",    "Historial de compras",    "/landing/mis-facturas",    "#6ee7b7", "rgba(16,185,129,.25)"),
                        new("bi-bell",           "Notificaciones",  "Mis avisos",              "/PacienteDashboard/Notificaciones", "#fcd34d", "rgba(245,158,11,.25)"),
                    }
                }
            },

            _ => new()
        };

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public record ModuloItem(
        string Icon,
        string Label,
        string Sub,
        string Url,
        string Color,
        string Bg
    );

    public class ModuloGroup
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<ModuloItem> Items { get; set; } = new();
    }
}
