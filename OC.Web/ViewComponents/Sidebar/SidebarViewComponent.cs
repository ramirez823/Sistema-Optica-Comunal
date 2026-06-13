using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace OC.Web.ViewComponents.Sidebar
{
    public class SidebarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var user = HttpContext.User;
            var groups = new List<SidebarGroup>();

            if (user.IsInRole("Admin"))
            {
                groups.Add(new SidebarGroup
                {
                    Title = "Inicio",
                    Icon = "bi-speedometer2",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Dashboard", Url = "/", Icon = "bi-speedometer2" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Gestión Clínica",
                    Icon = "bi-hospital",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" },
                        new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" },
                        new MenuItem { Title = "Órdenes Trabajo", Url = "/OrdenesTrabajo", Icon = "bi-eyeglasses" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Gestión Comercial",
                    Icon = "bi-cash-stack",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Ventas", Url = "/Ventas", Icon = "bi-receipt" },
                        new MenuItem { Title = "Inventario", Url = "/Inventory", Icon = "bi-box-seam" },
                        new MenuItem { Title = "Pedidos", Url = "/Pedidos", Icon = "bi-truck" },
                        new MenuItem { Title = "Proveedores", Url = "/Proveedores", Icon = "bi-building-check" },
                        new MenuItem { Title = "Sucursales", Url = "/Sucursales", Icon = "bi-geo-alt" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Usuarios", Url = "/Usuarios", Icon = "bi-shield-lock" },
                        new MenuItem { Title = "Planillas", Url = "/Planillas", Icon = "bi-calculator" },
                        new MenuItem { Title = "Asistencia", Url = "/Asistencia", Icon = "bi-clock" },
                        new MenuItem { Title = "Permiso", Url = "/Permiso", Icon = "bi-clock" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Reportes",
                    Icon = "bi-bar-chart",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Reportes", Url = "/Reportes", Icon = "bi-bar-chart-line" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" },
                        new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" },
                        new MenuItem { Title = "Panel Técnico", Url = "/Tickets/PanelTecnico", Icon = "bi-tools" },
                        new MenuItem { Title = "Gestionar Tickets", Url = "/Tickets", Icon = "bi-gear" },
                        new MenuItem { Title = "Equipos TI", Url = "/Equipos", Icon = "bi-pc-display" }
                    }
                });
            }
            else if (user.IsInRole("Optometrista"))
            {
                groups.Add(new SidebarGroup
                {
                    Title = "Inicio",
                    Icon = "bi-speedometer2",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Dashboard", Url = "/", Icon = "bi-speedometer2" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Gestión Clínica",
                    Icon = "bi-hospital",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" },
                        new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" },
                        new MenuItem { Title = "Órdenes Trabajo", Url = "/OrdenesTrabajo", Icon = "bi-eyeglasses" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" },
                        new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Mis Planillas", Url = "/Planillas/MisPlanillas", Icon = "bi-file-earmark-text" },
                        new MenuItem { Title = "Asistencia", Url = "/Asistencia", Icon = "bi-clock" },
                        new MenuItem { Title = "Permiso", Url = "/Permiso", Icon = "bi-clock" }
                    }
                });
            }
            else if (user.IsInRole("Recepcion"))
            {
                groups.Add(new SidebarGroup
                {
                    Title = "Inicio",
                    Icon = "bi-speedometer2",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Dashboard", Url = "/", Icon = "bi-speedometer2" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Gestión Clínica",
                    Icon = "bi-hospital",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Pacientes", Url = "/Pacientes", Icon = "bi-person-heart" },
                        new MenuItem { Title = "Citas", Url = "/CitasPublicas/CitasPaciente", Icon = "bi-calendar-event" },
                        new MenuItem { Title = "Órdenes Trabajo", Url = "/OrdenesTrabajo", Icon = "bi-eyeglasses" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Gestión Comercial",
                    Icon = "bi-cash-stack",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Ventas", Url = "/Ventas", Icon = "bi-receipt" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" },
                        new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Mis Planillas", Url = "/Planillas/MisPlanillas", Icon = "bi-file-earmark-text" },
                        new MenuItem { Title = "Asistencia", Url = "/Asistencia", Icon = "bi-clock" },
                        new MenuItem { Title = "Permiso", Url = "/Permiso", Icon = "bi-clock" }
                    }
                });
            }
            else if (user.IsInRole("Tecnico"))
            {
                groups.Add(new SidebarGroup
                {
                    Title = "Inicio",
                    Icon = "bi-speedometer2",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Dashboard", Url = "/", Icon = "bi-speedometer2" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Mesa de Ayuda",
                    Icon = "bi-headset",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Nuevo Ticket", Url = "/Tickets/Create", Icon = "bi-plus-circle" },
                        new MenuItem { Title = "Mis Tickets", Url = "/Tickets/MisTickets", Icon = "bi-ticket" },
                        new MenuItem { Title = "Panel Técnico", Url = "/Tickets/PanelTecnico", Icon = "bi-tools" },
                        new MenuItem { Title = "Equipos TI", Url = "/Equipos", Icon = "bi-pc-display" }
                    }
                });

                groups.Add(new SidebarGroup
                {
                    Title = "Recursos Humanos",
                    Icon = "bi-people",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Mis Planillas", Url = "/Planillas/MisPlanillas", Icon = "bi-file-earmark-text" },
                        new MenuItem { Title = "Permiso", Url = "/Permiso", Icon = "bi-clock" }
                    }
                });
            }
            else if (user.IsInRole("Paciente"))
            {
                groups.Add(new SidebarGroup
                {
                    Title = "Mi Cuenta",
                    Icon = "bi-person",
                    Items = new List<MenuItem>
                    {
                        new MenuItem { Title = "Mis Citas", Url = "/PacienteDashboard", Icon = "bi-calendar-event" },
                        new MenuItem { Title = "Solicitar Cita", Url = "/PacienteDashboard/AgendarCita", Icon = "bi-calendar-plus" },
                        new MenuItem { Title = "Estado de Orden", Url = "/PacienteDashboard/EstadoOrden", Icon = "bi-eyeglasses" },
                        new MenuItem { Title = "Mis Facturas", Url = "/landing/mis-facturas", Icon = "bi-receipt" },
                        new MenuItem { Title = "Notificaciones", Url = "/PacienteDashboard/Notificaciones", Icon = "bi-bell" }
                    }
                });
            }

            return View(groups);
        }
    }

    // Clases del modelo
    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class SidebarGroup
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<MenuItem> Items { get; set; } = new();
    }
}