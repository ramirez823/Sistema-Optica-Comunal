using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OC.Core.Domain.Entities;
using System.Security.Claims;
using OC.Data.Context;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Optometrista,Recepcion,Tecnico")]
    public class PermisoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] ExtensionesDocIncapacidad = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long TamanoMaxDocBytes = 10 * 1024 * 1024;

        public PermisoController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Permiso> query = _context.Permisos
                .Include(p => p.Usuario)
                .Include(p => p.AprobadoPor);

            if (role != "Admin")
            {
                query = query.Where(p => p.UsuarioId == userId);
            }

            const int pageSize = 10;
            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.FechaSolicitud)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new OC.Core.Common.PagedResult<Permiso>(
                items,
                totalItems,
                page,
                pageSize
            );

            return View(result);
        }

        public async Task<IActionResult> Create()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Admin")
            {
                ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(nameof(Permiso.UsuarioId), nameof(Permiso.Tipo), nameof(Permiso.Motivo), nameof(Permiso.FechaInicio), nameof(Permiso.FechaFin))]
            Permiso permiso,
            IFormFile? documentoIncapacidad)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            documentoIncapacidad ??= Request.Form.Files.GetFile("documentoIncapacidad");

            if (role != "Admin")
            {
                permiso.UsuarioId = userId;
            }
            else if (permiso.UsuarioId <= 0)
            {
                ModelState.AddModelError(nameof(permiso.UsuarioId), "Seleccione un usuario.");
            }

            if (permiso.FechaFin < permiso.FechaInicio)
            {
                ModelState.AddModelError(nameof(permiso.FechaFin), "La fecha fin no puede ser anterior a la fecha inicio.");
            }

            var esIncapacidad = string.Equals(permiso.Tipo, "Incapacidad", StringComparison.OrdinalIgnoreCase);

            if (esIncapacidad)
            {
                if (documentoIncapacidad == null || documentoIncapacidad.Length == 0)
                {
                    ModelState.AddModelError(nameof(documentoIncapacidad), "Debe adjuntar el documento de incapacidad (PDF o imagen).");
                }
            }
            else if (documentoIncapacidad != null && documentoIncapacidad.Length > 0)
            {
                ModelState.AddModelError(nameof(documentoIncapacidad), "El documento adjunto solo aplica al tipo «Incapacidad».");
            }

            if (documentoIncapacidad != null && documentoIncapacidad.Length > 0)
            {
                if (documentoIncapacidad.Length > TamanoMaxDocBytes)
                {
                    ModelState.AddModelError(nameof(documentoIncapacidad), "El archivo no puede superar 10 MB.");
                }

                var ext = Path.GetExtension(documentoIncapacidad.FileName).ToLowerInvariant();
                if (!ExtensionesDocIncapacidad.Contains(ext))
                {
                    ModelState.AddModelError(nameof(documentoIncapacidad), "Formato no permitido. Use PDF, JPG o PNG.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (role == "Admin")
                {
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                }

                return View(permiso);
            }

            permiso.Estado = "Pendiente";
            permiso.FechaSolicitud = DateTime.Now;
            permiso.RutaDocumentoIncapacidad = null;

            if (esIncapacidad && documentoIncapacidad != null && documentoIncapacidad.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? "", "uploads", "permisos-incapacidad");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var ext = Path.GetExtension(documentoIncapacidad.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
                var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await documentoIncapacidad.CopyToAsync(stream);
                }

                permiso.RutaDocumentoIncapacidad = "/uploads/permisos-incapacidad/" + uniqueFileName;
            }

            _context.Permisos.Add(permiso);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Aprobar(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var permiso = await _context.Permisos.FindAsync(id);

            if (permiso == null)
                return NotFound();

            permiso.Estado = "Aprobado";
            permiso.AprobadoPorId = userId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Rechazar(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);

            var permiso = await _context.Permisos.FindAsync(id);

            if (permiso == null)
                return NotFound();

            permiso.Estado = "Rechazado";
            permiso.AprobadoPorId = userId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
