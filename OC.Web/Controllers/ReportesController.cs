using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportesController : Controller
    {
        private readonly IGenericRepository<Venta> _ventaRepo;
        private readonly IGenericRepository<Cita> _citaRepo;
        private readonly IGenericRepository<Sucursal> _sucursalRepo;

        public ReportesController(
            IGenericRepository<Venta> ventaRepo,
            IGenericRepository<Cita> citaRepo,
            IGenericRepository<Sucursal> sucursalRepo)
        {
            _ventaRepo = ventaRepo;
            _citaRepo = citaRepo;
            _sucursalRepo = sucursalRepo;
        }

        public IActionResult Index() => View();

        // ── VENTAS ──────────────────────────────────────────────────────────

        public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-1);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var sucursales = (await _sucursalRepo.GetPagedAsync(1, 100)).Items.ToList();

                var raw = await _ventaRepo.GetPagedAsync(
                    1, 10000,
                    v => v.FechaVenta >= fechaDesde && v.FechaVenta <= fechaHasta
                         && (!sucursalId.HasValue || v.SucursalId == sucursalId),
                    includeProperties: "Paciente,Usuario,Sucursal,Detalles");

                var ventas = raw.Items.ToList();

                var porFecha = ventas
                    .GroupBy(v => v.FechaVenta.Date)
                    .OrderBy(g => g.Key)
                    .ToList();

                var porSucursal = ventas
                    .GroupBy(v => v.Sucursal?.Nombre ?? "Sin sucursal")
                    .OrderBy(g => g.Key)
                    .ToList();

                var porMetodo = ventas
                    .GroupBy(v => v.MetodoPago.ToString())
                    .ToList();

                var topProductos = ventas
                    .SelectMany(v => v.Detalles)
                    .GroupBy(d => d.DescripcionSnapshot)
                    .Select(g => new ProductoTopItem
                    {
                        Nombre = g.Key,
                        CantidadVendida = g.Sum(d => d.Cantidad),
                        TotalGenerado = g.Sum(d => d.Subtotal)
                    })
                    .OrderByDescending(x => x.TotalGenerado)
                    .Take(10)
                    .ToList();

                var vm = new ReporteVentasViewModel
                {
                    Desde = fechaDesde,
                    Hasta = hasta ?? DateTime.Now,
                    SucursalId = sucursalId,
                    Sucursales = sucursales,
                    TotalGeneral = ventas.Sum(v => v.Total),
                    TotalTransacciones = ventas.Count,
                    PromedioVenta = ventas.Any() ? ventas.Average(v => v.Total) : 0,
                    EtiquetasFecha = porFecha.Select(g => g.Key.ToString("dd/MM")).ToList(),
                    TotalesPorFecha = porFecha.Select(g => g.Sum(v => v.Total)).ToList(),
                    EtiquetasSucursal = porSucursal.Select(g => g.Key).ToList(),
                    TotalesPorSucursal = porSucursal.Select(g => g.Sum(v => v.Total)).ToList(),
                    EtiquetasMetodo = porMetodo.Select(g => g.Key).ToList(),
                    ConteosPorMetodo = porMetodo.Select(g => g.Count()).ToList(),
                    TopProductos = topProductos
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportarVentasExcel(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-1);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var raw = await _ventaRepo.GetPagedAsync(
                    1, 10000,
                    v => v.FechaVenta >= fechaDesde && v.FechaVenta <= fechaHasta
                         && (!sucursalId.HasValue || v.SucursalId == sucursalId),
                    includeProperties: "Paciente,Usuario,Sucursal,Detalles");

                var ventas = raw.Items.ToList();

                using var wb = new XLWorkbook();

                // ── Hoja 1: Listado ─────────────────────────────────────────
                var ws = wb.Worksheets.Add("Ventas");

                // Paleta
                var colorPrimario = XLColor.FromHtml("#1a3c6e");
                var colorSecundario = XLColor.FromHtml("#2e6db4");
                var colorAcento = XLColor.FromHtml("#e8f0fb");
                var colorFila = XLColor.FromHtml("#f4f6fb");
                var colorTotal = XLColor.FromHtml("#d0e4ff");

                // ── Título ──────────────────────────────────────────────────
                ws.Cell(1, 1).Value = "ÓPTICA COMUNAL";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 18;
                ws.Cell(1, 1).Style.Font.FontColor = colorPrimario;
                ws.Range(1, 1, 1, 9).Merge();

                ws.Cell(2, 1).Value = $"Reporte de Ventas  |  {fechaDesde:dd/MM/yyyy} — {(hasta ?? DateTime.Now):dd/MM/yyyy}";
                ws.Cell(2, 1).Style.Font.FontSize = 11;
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                ws.Range(2, 1, 2, 9).Merge();

                ws.Row(1).Height = 28;
                ws.Row(2).Height = 18;

                // ── Resumen (fila 4) ────────────────────────────────────────
                var resumenData = new[]
                {
            ("TRANSACCIONES",  ventas.Count.ToString()),
            ("TOTAL VENTAS",   $"₡{ventas.Sum(v => v.Total):N2}"),
            ("PROMEDIO",       $"₡{(ventas.Any() ? ventas.Average(v => v.Total) : 0):N2}")
        };

                int col = 1;
                foreach (var (label, valor) in resumenData)
                {
                    var rLabel = ws.Cell(4, col);
                    rLabel.Value = label;
                    rLabel.Style.Font.Bold = true;
                    rLabel.Style.Font.FontSize = 8;
                    rLabel.Style.Font.FontColor = XLColor.White;
                    rLabel.Style.Fill.BackgroundColor = colorSecundario;
                    rLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var rValor = ws.Cell(5, col);
                    rValor.Value = valor;
                    rValor.Style.Font.Bold = true;
                    rValor.Style.Font.FontSize = 13;
                    rValor.Style.Fill.BackgroundColor = colorAcento;
                    rValor.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Range(4, col, 4, col + 2).Merge();
                    ws.Range(5, col, 5, col + 2).Merge();
                    col += 3;
                }
                ws.Row(4).Height = 16;
                ws.Row(5).Height = 26;

                // ── Encabezados tabla (fila 7) ──────────────────────────────
                string[] headers = { "Factura", "Fecha", "Paciente", "Teléfono", "Correo", "Cajero", "Sucursal", "Método", "Total (₡)" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(7, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = colorPrimario;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.White;
                }
                ws.Row(7).Height = 20;

                // ── Datos ───────────────────────────────────────────────────
                int row = 8;
                foreach (var v in ventas)
                {
                    ws.Cell(row, 1).Value = v.NumeroFactura;
                    ws.Cell(row, 2).Value = v.FechaVenta.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 3).Value = v.Paciente?.NombreCompleto;
                    ws.Cell(row, 4).Value = v.Paciente?.Telefono ?? "";
                    ws.Cell(row, 5).Value = v.Paciente?.Email ?? "";
                    ws.Cell(row, 6).Value = v.Usuario?.Nombre;
                    ws.Cell(row, 7).Value = v.Sucursal?.Nombre;
                    ws.Cell(row, 8).Value = v.MetodoPago.ToString();
                    ws.Cell(row, 9).Value = v.Total;
                    ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

                    var bg = row % 2 == 0 ? colorFila : XLColor.White;
                    ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = bg;
                    ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    ws.Range(row, 1, row, 9).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c5cfe0");

                    row++;
                }

                // ── Fila total ──────────────────────────────────────────────
                ws.Range(row, 1, row, 8).Merge();
                ws.Cell(row, 1).Value = $"TOTAL  ({ventas.Count} transacciones)";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 9).Value = ventas.Sum(v => v.Total);
                ws.Cell(row, 9).Style.Font.Bold = true;
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = colorTotal;
                ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

                ws.SheetView.FreezeRows(7);
                ws.Columns().AdjustToContents();

                // ── Hoja 2: Por método ──────────────────────────────────────
                var ws2 = wb.Worksheets.Add("Por Método");
                ws2.Cell(1, 1).Value = "Ventas por Método de Pago";
                ws2.Cell(1, 1).Style.Font.Bold = true;
                ws2.Cell(1, 1).Style.Font.FontSize = 14;
                ws2.Cell(1, 1).Style.Font.FontColor = colorPrimario;
                ws2.Range(1, 1, 1, 4).Merge();

                string[] h2 = { "Método", "Transacciones", "Total (₡)", "Promedio (₡)" };
                for (int i = 0; i < h2.Length; i++)
                {
                    ws2.Cell(3, i + 1).Value = h2[i];
                    ws2.Cell(3, i + 1).Style.Font.Bold = true;
                    ws2.Cell(3, i + 1).Style.Fill.BackgroundColor = colorPrimario;
                    ws2.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
                    ws2.Cell(3, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int r2 = 4;
                foreach (var g in ventas.GroupBy(v => v.MetodoPago.ToString()))
                {
                    ws2.Cell(r2, 1).Value = g.Key;
                    ws2.Cell(r2, 2).Value = g.Count();
                    ws2.Cell(r2, 3).Value = g.Sum(v => v.Total);
                    ws2.Cell(r2, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws2.Cell(r2, 4).Value = g.Average(v => v.Total);
                    ws2.Cell(r2, 4).Style.NumberFormat.Format = "#,##0.00";
                    ws2.Range(r2, 1, r2, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    if (r2 % 2 == 0)
                        ws2.Range(r2, 1, r2, 4).Style.Fill.BackgroundColor = colorFila;
                    r2++;
                }
                ws2.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteVentas_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction("Ventas", new { desde, hasta, sucursalId });
            }
        }

        // ── FIDELIZACIÓN ────────────────────────────────────────────────────

        public async Task<IActionResult> Fidelizacion(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-12);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
                var haceUnMes = fechaHasta.AddMonths(-1);

                var sucursales = (await _sucursalRepo.GetPagedAsync(1, 100)).Items.ToList();

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && c.Estado == EstadoCita.Atendida
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId),
                    includeProperties: "Paciente");

                var detalle = raw.Items
                    .GroupBy(c => c.PacienteId)
                    .Select(g =>
                    {
                        var paciente = g.First().Paciente;
                        int visitas = g.Count();
                        bool esNuevo = paciente.FechaRegistro >= haceUnMes;

                        string clasificacion = esNuevo
                            ? "Nuevo"
                            : visitas >= 4
                                ? "Frecuente"
                                : visitas >= 2
                                    ? "Regular"
                                    : "Esporádico";

                        return new FidelizacionPacienteItem
                        {
                            NombreCompleto = paciente.NombreCompleto,
                            Cedula = paciente.Cedula,
                            Telefono = paciente.Telefono,
                            Email = paciente.Email,
                            VisitasEnPeriodo = visitas,
                            Clasificacion = clasificacion,
                            UltimaVisita = g.Max(c => c.FechaHora)
                        };
                    })
                    .OrderByDescending(x => x.VisitasEnPeriodo)
                    .ToList();

                var vm = new ReporteFidelizacionViewModel
                {
                    Desde = desde ?? DateTime.Now.AddMonths(-12),
                    Hasta = hasta ?? DateTime.Now,
                    SucursalId = sucursalId,
                    Sucursales = sucursales,
                    TotalNuevos = detalle.Count(x => x.Clasificacion == "Nuevo"),
                    TotalRegulares = detalle.Count(x => x.Clasificacion == "Regular"),
                    TotalFrecuentes = detalle.Count(x => x.Clasificacion == "Frecuente"),
                    TotalEsporadicos = detalle.Count(x => x.Clasificacion == "Esporádico"),
                    Detalle = detalle
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportarFidelizacionExcel(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-12);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);
                var haceUnMes = fechaHasta.AddMonths(-1);

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && c.Estado == EstadoCita.Atendida
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId),
                    includeProperties: "Paciente");

                var detalle = raw.Items
                    .GroupBy(c => c.PacienteId)
                    .Select(g =>
                    {
                        var paciente = g.First().Paciente;
                        int visitas = g.Count();
                        bool esNuevo = paciente.FechaRegistro >= haceUnMes;
                        string cls = esNuevo ? "Nuevo" : visitas >= 4 ? "Frecuente" : visitas >= 2 ? "Regular" : "Esporádico";
                        return new FidelizacionPacienteItem
                        {
                            NombreCompleto = paciente.NombreCompleto,
                            Cedula = paciente.Cedula,
                            Telefono = paciente.Telefono,
                            Email = paciente.Email,
                            VisitasEnPeriodo = visitas,
                            Clasificacion = cls,
                            UltimaVisita = g.Max(c => c.FechaHora)
                        };
                    })
                    .OrderByDescending(x => x.VisitasEnPeriodo)
                    .ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Fidelización");

                // ── Paleta ──────────────────────────────────────────────────
                var colorPrimario = XLColor.FromHtml("#1a3c6e");
                var colorSecundario = XLColor.FromHtml("#2e6db4");
                var colorAcento = XLColor.FromHtml("#e8f0fb");

                // ── Título ──────────────────────────────────────────────────
                ws.Cell(1, 1).Value = "ÓPTICA COMUNAL";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 18;
                ws.Cell(1, 1).Style.Font.FontColor = colorPrimario;
                ws.Range(1, 1, 1, 7).Merge();

                ws.Cell(2, 1).Value = $"Reporte de Fidelización  |  {fechaDesde:dd/MM/yyyy} — {(hasta ?? DateTime.Now):dd/MM/yyyy}";
                ws.Cell(2, 1).Style.Font.FontSize = 11;
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                ws.Range(2, 1, 2, 7).Merge();

                ws.Row(1).Height = 28;
                ws.Row(2).Height = 18;

                // ── Tarjetas de resumen (filas 4–5) ─────────────────────────
                var tarjetas = new[]
                {
            ("NUEVOS",      detalle.Count(x => x.Clasificacion == "Nuevo").ToString(),
             "#2e6db4", "#e8f0fb"),

            ("REGULARES",   detalle.Count(x => x.Clasificacion == "Regular").ToString(),
             "#2e6db4", "#cce5ff"),

            ("FRECUENTES",  detalle.Count(x => x.Clasificacion == "Frecuente").ToString(),
             "#196f3d", "#d4edda"),

            ("ESPORÁDICOS", detalle.Count(x => x.Clasificacion == "Esporádico").ToString(),
             "#7d6608", "#fff3cd"),
        };

                int col = 1;
                foreach (var (label, valor, bgLabel, bgValor) in tarjetas)
                {
                    ws.Cell(4, col).Value = label;
                    ws.Cell(4, col).Style.Font.Bold = true;
                    ws.Cell(4, col).Style.Font.FontSize = 8;
                    ws.Cell(4, col).Style.Font.FontColor = XLColor.White;
                    ws.Cell(4, col).Style.Fill.BackgroundColor = XLColor.FromHtml(bgLabel);
                    ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(5, col).Value = valor;
                    ws.Cell(5, col).Style.Font.Bold = true;
                    ws.Cell(5, col).Style.Font.FontSize = 14;
                    ws.Cell(5, col).Style.Fill.BackgroundColor = XLColor.FromHtml(bgValor);
                    ws.Cell(5, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Range(4, col, 4, col + 1).Merge();
                    ws.Range(5, col, 5, col + 1).Merge();
                    col += 2;
                }

                ws.Row(4).Height = 16;
                ws.Row(5).Height = 26;

                // ── Encabezados tabla (fila 7) ──────────────────────────────
                string[] headers = { "Paciente", "Cédula", "Teléfono", "Correo", "Visitas", "Última Visita", "Clasificación" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(7, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = colorPrimario;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.White;
                }
                ws.Row(7).Height = 20;

                // ── Datos ───────────────────────────────────────────────────
                int row = 8;
                foreach (var p in detalle)
                {
                    ws.Cell(row, 1).Value = p.NombreCompleto;
                    ws.Cell(row, 2).Value = p.Cedula;
                    ws.Cell(row, 3).Value = p.Telefono ?? "";
                    ws.Cell(row, 4).Value = p.Email ?? "";
                    ws.Cell(row, 5).Value = p.VisitasEnPeriodo;
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(row, 6).Value = p.UltimaVisita.ToString("dd/MM/yyyy");
                    ws.Cell(row, 7).Value = p.Clasificacion;
                    ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var color = p.Clasificacion switch
                    {
                        "Frecuente" => XLColor.FromHtml("#d4edda"),
                        "Regular" => XLColor.FromHtml("#cce5ff"),
                        "Nuevo" => XLColor.FromHtml("#fff3cd"),
                        _ => XLColor.FromHtml("#f8f9fa")
                    };
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = color;
                    ws.Range(row, 1, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    ws.Range(row, 1, row, 7).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c5cfe0");

                    row++;
                }

                ws.SheetView.FreezeRows(7);
                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteFidelizacion_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction("Fidelizacion", new { desde, hasta, sucursalId });
            }
        }

        // ── DEMANDA ─────────────────────────────────────────────────────────

        public async Task<IActionResult> Demanda(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-3);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var sucursales = (await _sucursalRepo.GetPagedAsync(1, 100)).Items.ToList();

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId));

                var citas = raw.Items.ToList();

                var porMes = citas
                    .GroupBy(c => new { c.FechaHora.Year, c.FechaHora.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToList();

                var vm = new ReporteDemandaViewModel
                {
                    Desde = desde ?? DateTime.Now.AddMonths(-3),
                    Hasta = hasta ?? DateTime.Now,
                    SucursalId = sucursalId,
                    Sucursales = sucursales,
                    TotalAgendadas = citas.Count,
                    TotalAtendidas = citas.Count(c => c.Estado == EstadoCita.Atendida),
                    TotalCanceladas = citas.Count(c => c.Estado == EstadoCita.Cancelada),
                    TotalPendientes = citas.Count(c => c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada),
                    EtiquetasMes = porMes.Select(g => $"{g.Key.Month:00}/{g.Key.Year}").ToList(),
                    AtendidaPorMes = porMes.Select(g => g.Count(c => c.Estado == EstadoCita.Atendida)).ToList(),
                    CanceladaPorMes = porMes.Select(g => g.Count(c => c.Estado == EstadoCita.Cancelada)).ToList(),
                    PendientePorMes = porMes.Select(g => g.Count(c => c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada)).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportarDemandaExcel(DateTime? desde, DateTime? hasta, int? sucursalId)
        {
            try
            {
                var fechaDesde = desde ?? DateTime.Now.AddMonths(-3);
                var fechaHasta = (hasta ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

                var raw = await _citaRepo.GetPagedAsync(
                    1, 10000,
                    c => c.FechaHora >= fechaDesde && c.FechaHora <= fechaHasta
                         && (!sucursalId.HasValue || c.SucursalId == sucursalId),
                    includeProperties: "Paciente,Sucursal");

                var citas = raw.Items.OrderBy(c => c.FechaHora).ToList();

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Demanda");

                var colorPrimario = XLColor.FromHtml("#1a3c6e");
                var colorSecundario = XLColor.FromHtml("#2e6db4");
                var colorAcento = XLColor.FromHtml("#e8f0fb");
                var colorFila = XLColor.FromHtml("#f4f6fb");

                // ── Título ──────────────────────────────────────────────────
                ws.Cell(1, 1).Value = "ÓPTICA COMUNAL";
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 18;
                ws.Cell(1, 1).Style.Font.FontColor = colorPrimario;
                ws.Range(1, 1, 1, 7).Merge();

                ws.Cell(2, 1).Value = $"Reporte de Demanda  |  {fechaDesde:dd/MM/yyyy} — {(hasta ?? DateTime.Now):dd/MM/yyyy}";
                ws.Cell(2, 1).Style.Font.FontSize = 11;
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                ws.Range(2, 1, 2, 7).Merge();

                ws.Row(1).Height = 28;
                ws.Row(2).Height = 18;

                // ── Resumen (fila 4) ────────────────────────────────────────
                var atendidas = citas.Count(c => c.Estado == EstadoCita.Atendida);
                var canceladas = citas.Count(c => c.Estado == EstadoCita.Cancelada);
                var pendientes = citas.Count(c => c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada);

                var resumenData = new[]
                         {
                            ("TOTAL CITAS", citas.Count.ToString()),
                            ("ATENDIDAS",   atendidas.ToString()),
                        };

                int col = 1;
                foreach (var (label, valor) in resumenData)
                {
                    ws.Cell(4, col).Value = label;
                    ws.Cell(4, col).Style.Font.Bold = true;
                    ws.Cell(4, col).Style.Font.FontSize = 8;
                    ws.Cell(4, col).Style.Font.FontColor = XLColor.White;
                    ws.Cell(4, col).Style.Fill.BackgroundColor = colorSecundario;
                    ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(5, col).Value = valor;
                    ws.Cell(5, col).Style.Font.Bold = true;
                    ws.Cell(5, col).Style.Font.FontSize = 14;
                    ws.Cell(5, col).Style.Fill.BackgroundColor = colorAcento;
                    ws.Cell(5, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Range(4, col, 4, col + 1).Merge();
                    ws.Range(5, col, 5, col + 1).Merge();
                    col += 2;
                }

                // CANCELADAS — rojo
                ws.Cell(4, col).Value = "CANCELADAS";
                ws.Cell(4, col).Style.Font.Bold = true;
                ws.Cell(4, col).Style.Font.FontSize = 8;
                ws.Cell(4, col).Style.Font.FontColor = XLColor.White;
                ws.Cell(4, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#c0392b");
                ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(5, col).Value = canceladas.ToString();
                ws.Cell(5, col).Style.Font.Bold = true;
                ws.Cell(5, col).Style.Font.FontSize = 14;
                ws.Cell(5, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
                ws.Cell(5, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(4, col, 4, col + 1).Merge();
                ws.Range(5, col, 5, col + 1).Merge();
                col += 2;


                // Pendientes con color amarillo
                ws.Cell(4, col).Value = "PENDIENTES";
                ws.Cell(4, col).Style.Font.Bold = true;
                ws.Cell(4, col).Style.Font.FontSize = 8;
                ws.Cell(4, col).Style.Font.FontColor = XLColor.White;
                ws.Cell(4, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#e6a817");
                ws.Cell(4, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(5, col).Value = pendientes.ToString();
                ws.Cell(5, col).Style.Font.Bold = true;
                ws.Cell(5, col).Style.Font.FontSize = 14;
                ws.Cell(5, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
                ws.Cell(5, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(4, col, 4, col + 1).Merge();
                ws.Range(5, col, 5, col + 1).Merge();

                ws.Row(4).Height = 16;
                ws.Row(5).Height = 26;

                // ── Encabezados tabla (fila 7) ──────────────────────────────
                string[] headers = { "Fecha", "Paciente", "Teléfono", "Correo", "Sucursal", "Estado", "Motivo" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(7, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = colorPrimario;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.White;
                }
                ws.Row(7).Height = 20;

                // ── Datos ───────────────────────────────────────────────────
                int row = 8;
                foreach (var c in citas)
                {
                    ws.Cell(row, 1).Value = c.FechaHora.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 2).Value = c.Paciente?.NombreCompleto;
                    ws.Cell(row, 3).Value = c.Paciente?.Telefono ?? "";
                    ws.Cell(row, 4).Value = c.Paciente?.Email ?? "";
                    ws.Cell(row, 5).Value = c.Sucursal?.Nombre;
                    ws.Cell(row, 6).Value = c.Estado.ToString();
                    ws.Cell(row, 7).Value = c.MotivoConsulta;

                    var color = c.Estado switch
                    {
                        EstadoCita.Atendida => XLColor.FromHtml("#d4edda"),
                        EstadoCita.Cancelada => XLColor.FromHtml("#f8d7da"),
                        _ => XLColor.FromHtml("#fff3cd")
                    };
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = color;
                    ws.Range(row, 1, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    ws.Range(row, 1, row, 7).Style.Border.OutsideBorderColor = XLColor.FromHtml("#c5cfe0");

                    row++;
                }

                ws.SheetView.FreezeRows(7);
                ws.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                wb.SaveAs(stream);
                return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReporteDemanda_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar: {ex.Message}";
                return RedirectToAction("Demanda", new { desde, hasta, sucursalId });
            }
        }
    }
}