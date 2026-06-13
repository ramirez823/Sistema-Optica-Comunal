using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Web.ViewModels;
using System.Security.Claims;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Optometrista,Recepcion")]
    public class AsistenciaController : Controller
    {
        private readonly AppDbContext _context;

        public AsistenciaController(AppDbContext context)
        {
            _context = context;
        }

        /*public async Task<IActionResult> Index(DateTime? fecha)
        {
            var hoy = fecha ?? DateTime.Today;

            var data = await _context.Asistencias
                .Where(a => a.Fecha == hoy)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();

            return View(data);
        }*/

        /*public async Task<IActionResult> Index(DateTime? fecha)
        {
            var hoy = fecha ?? DateTime.Today;

            var userId = int.Parse(User.FindFirst("UserId").Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Asistencia> query = _context.Asistencias;

            if (role != "Admin")
            {
                query = query.Where(a => a.UsuarioId == userId);
            }

            var data = await query
                .Include(a => a.Usuario)
                .Where(a => a.Fecha == hoy)
                .ToListAsync();

            return View(data);
        }*/

        public async Task<IActionResult> Index(DateTime? fecha)
        {
            var userId = int.Parse(User.FindFirst("UserId").Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Asistencia> query = _context.Asistencias
                .Include(a => a.Usuario);

            if (role != "Admin")
            {
                query = query.Where(a => a.UsuarioId == userId);
            }

            
            if (fecha.HasValue)
            {
                query = query.Where(a => a.Fecha == fecha.Value);
            }

            var data = await query
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();

            return View(data);
        }

        [Authorize(Roles = "Optometrista,Recepcion")]
        public async Task<IActionResult> MarcarEntrada()
        {
            var usuarioId = int.Parse(User.FindFirst("UserId").Value);

            var hoy = DateTime.Today;

            var asistencia = await _context.Asistencias
            .Where(a => a.UsuarioId == usuarioId && a.Fecha == hoy)
            .FirstOrDefaultAsync();

            if (asistencia != null)
            {
                return BadRequest("Ya marcó entrada hoy.");
            }

            var nueva = new Asistencia
            {
                UsuarioId = usuarioId,
                Fecha = hoy,
                HoraEntrada = DateTime.Now
            };

            _context.Asistencias.Add(nueva);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Optometrista,Recepcion")]
        public async Task<IActionResult> MarcarSalida()
        {
            var usuarioId = int.Parse(User.FindFirst("UserId").Value);
            var hoy = DateTime.Today;

            var asistencia = await _context.Asistencias
                .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId && a.Fecha == hoy);

            if (asistencia == null || asistencia.HoraEntrada == null)
            {
                return BadRequest("No ha marcado entrada.");
            }

            if (asistencia.HoraSalida != null)
            {
                return BadRequest("Ya marcó salida.");
            }

            asistencia.HoraSalida = DateTime.Now;

            _context.Asistencias.Update(asistencia);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}