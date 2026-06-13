using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.Helpers;
using OC.Web.Services;
using OC.Web.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Recepcion,Optometrista")]
    public class PacientesController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Usuario> _usuariosRepo;
        private readonly ITotpService _totpService;
        private const string RegistroPendienteSessionKey = "RegistroPacientePendiente";
        private const string RegistroTotpSecretSessionKey = "RegistroPacienteTotpSecret";

        public PacientesController(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Usuario> usuariosRepo,
            ITotpService totpService)
        {
            _pacientesRepo = pacientesRepo;
            _usuariosRepo = usuariosRepo;
            _totpService = totpService;
        }

        // LISTAR con búsqueda por nombre o cédula
        public async Task<IActionResult> Index(string searchTerm, int page = 1)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Búsqueda por nombre o cédula (insensible a mayúsculas)
                var results = await _pacientesRepo.GetPagedAsync(
                    pageIndex: page,
                    pageSize: 100, // Mostrar todos los resultados en una página
                    filter: p => p.Nombres.Contains(searchTerm) || p.Apellidos.Contains(searchTerm) || p.Cedula.Contains(searchTerm),
                    includeProperties: "Citas.Expediente" // Incluir citas y expedientes
                );
                ViewBag.SearchTerm = searchTerm;
                return View(results);
            }
            else
            {
                // Listado normal paginado
                var result = await _pacientesRepo.GetPagedAsync(
                    pageIndex: page,
                    pageSize: 10,
                    orderBy: q => q.OrderByDescending(p => p.FechaRegistro)
                );
                return View(result);
            }
        }

        // REGISTRO PÚBLICO (Sin autenticación)
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Registro()
        {
            HttpContext.Session.Remove(RegistroPendienteSessionKey);
            HttpContext.Session.Remove(RegistroTotpSecretSessionKey);
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(PacienteViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Contrasena))
                ModelState.AddModelError(nameof(model.Contrasena), "La contraseña es obligatoria.");

            var cedulaNormalizada = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNormalizada))
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View(model);
            }
            model.Cedula = cedulaNormalizada;

            if (!ModelState.IsValid)
                return View(model);

            await IdentidadDuplicadaValidation.ValidarCedulaUnicaAsync(
                _pacientesRepo, _usuariosRepo, model.Cedula, ModelState, nameof(model.Cedula));
            await IdentidadDuplicadaValidation.ValidarCorreoUnicoAsync(
                _pacientesRepo, _usuariosRepo, model.Email, ModelState, nameof(model.Email));
            if (!ModelState.IsValid)
                return View(model);

            HttpContext.Session.SetString(RegistroPendienteSessionKey, JsonSerializer.Serialize(model));
            var secret = _totpService.GenerateSecretBase32();
            HttpContext.Session.SetString(RegistroTotpSecretSessionKey, secret);

            return RedirectToAction(nameof(ConfigurarTotpRegistro));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ConfigurarTotpRegistro()
        {
            var modelJson = HttpContext.Session.GetString(RegistroPendienteSessionKey);
            var secret = HttpContext.Session.GetString(RegistroTotpSecretSessionKey);
            if (string.IsNullOrWhiteSpace(modelJson) || string.IsNullOrWhiteSpace(secret))
                return RedirectToAction(nameof(Registro));

            var model = JsonSerializer.Deserialize<PacienteViewModel>(modelJson);
            if (model == null || string.IsNullOrWhiteSpace(model.Email))
                return RedirectToAction(nameof(Registro));

            var issuer = "OpticaComunal";
            var label = $"{issuer}:{model.Email}";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
            var qrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=230x230&data=" + Uri.EscapeDataString(otpauth);

            ViewBag.Correo = model.Email;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(secret);
            ViewBag.QrUrl = qrUrl;
            return View(new ValidarTotpViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarTotpRegistro(ValidarTotpViewModel model)
        {
            var modelJson = HttpContext.Session.GetString(RegistroPendienteSessionKey);
            var secret = HttpContext.Session.GetString(RegistroTotpSecretSessionKey);
            if (string.IsNullOrWhiteSpace(modelJson) || string.IsNullOrWhiteSpace(secret))
                return RedirectToAction(nameof(Registro));

            var pendiente = JsonSerializer.Deserialize<PacienteViewModel>(modelJson);
            if (pendiente == null || string.IsNullOrWhiteSpace(pendiente.Email))
                return RedirectToAction(nameof(Registro));

            var issuer = "OpticaComunal";
            var label = $"{issuer}:{pendiente.Email}";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
            ViewBag.QrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=230x230&data=" + Uri.EscapeDataString(otpauth);
            ViewBag.Correo = pendiente.Email;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(secret);

            if (!ModelState.IsValid)
                return View(model);

            if (!_totpService.VerifyCode(secret, model.Codigo))
            {
                ModelState.AddModelError(string.Empty, "Código TOTP inválido o expirado.");
                return View(model);
            }

            // Revalidar unicidad por si cambió mientras configuraba TOTP.
            var cedulaNorm = CedulaValidation.Normalizar(pendiente.Cedula);
            var cedulaDupPac = await _pacientesRepo.GetPagedAsync(1, 1, p => p.Cedula == cedulaNorm);
            var cedulaDupUsr = await _usuariosRepo.GetPagedAsync(1, 1, u => u.Cedula == cedulaNorm);
            if (cedulaDupPac.Items.Any() || cedulaDupUsr.Items.Any())
            {
                HttpContext.Session.Remove(RegistroPendienteSessionKey);
                HttpContext.Session.Remove(RegistroTotpSecretSessionKey);
                TempData["Error"] = "No se pudo completar el registro: la cédula ya está registrada en el sistema.";
                return RedirectToAction(nameof(Registro));
            }

            var correoNorm = (pendiente.Email ?? string.Empty).Trim().ToLowerInvariant();
            var emailDupPac = await _pacientesRepo.GetPagedAsync(1, 1,
                p => p.Email != null && p.Email.ToLower() == correoNorm);
            var emailDupUsr = await _usuariosRepo.GetPagedAsync(1, 1, u => u.Correo.ToLower() == correoNorm);
            if (emailDupPac.Items.Any() || emailDupUsr.Items.Any())
            {
                HttpContext.Session.Remove(RegistroPendienteSessionKey);
                HttpContext.Session.Remove(RegistroTotpSecretSessionKey);
                TempData["Error"] = "No se pudo completar el registro: el correo ya está registrado en el sistema.";
                return RedirectToAction(nameof(Registro));
            }

            var entity = new Paciente
            {
                Nombres = pendiente.Nombres,
                Apellidos = pendiente.Apellidos,
                Cedula = cedulaNorm,
                Telefono = pendiente.Telefono,
                Email = pendiente.Email,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(pendiente.Contrasena!),
                FechaNacimiento = pendiente.FechaNacimiento,
                FechaRegistro = DateTime.Now,
                TotpSecretProtegido = _totpService.ProtectSecret(secret),
                TotpHabilitado = true,
                TotpConfiguradoEnUtc = DateTime.UtcNow
            };

            await _pacientesRepo.AddAsync(entity);
            HttpContext.Session.Remove(RegistroPendienteSessionKey);
            HttpContext.Session.Remove(RegistroTotpSecretSessionKey);

            // Login automático del paciente recién registrado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, entity.NombreCompleto),
                new Claim(ClaimTypes.Role, "Paciente"),
                new Claim("PacienteId", entity.Id.ToString()),
                new Claim("Cedula", entity.Cedula)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = false });

            TempData["Success"] = "Paciente registrado exitosamente. ¡Bienvenido!";
            return RedirectToAction("Index", "Landing");
        }

        // CREATE (Solo Admin y Recepcion)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PacienteViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Contrasena))
            {
                ModelState.AddModelError(nameof(model.Contrasena), "La contraseña es obligatoria.");
            }

            var cedulaNorm = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View(model);
            }
            model.Cedula = cedulaNorm;

            if (!ModelState.IsValid)
                return View(model);

            await IdentidadDuplicadaValidation.ValidarCedulaUnicaAsync(
                _pacientesRepo, _usuariosRepo, model.Cedula, ModelState, nameof(model.Cedula));
            await IdentidadDuplicadaValidation.ValidarCorreoUnicoAsync(
                _pacientesRepo, _usuariosRepo, model.Email, ModelState, nameof(model.Email));
            if (!ModelState.IsValid)
                return View(model);

            var entity = new Paciente
            {
                Nombres = model.Nombres,
                Apellidos = model.Apellidos,
                Cedula = cedulaNorm,
                Telefono = model.Telefono,
                Email = model.Email,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena!),
                FechaNacimiento = model.FechaNacimiento,
                FechaRegistro = DateTime.Now
            };

            await _pacientesRepo.AddAsync(entity);

            TempData["Success"] = "Paciente creado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _pacientesRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            ViewBag.Bloqueado = entity.BloqueadoPermanentemente || (entity.BloqueadoHastaUtc.HasValue && entity.BloqueadoHastaUtc.Value > DateTime.UtcNow);

            return View(new PacienteViewModel
            {
                Id = entity.Id,
                Nombres = entity.Nombres,
                Apellidos = entity.Apellidos,
                Cedula = CedulaValidation.FormatearParaMostrar(entity.Cedula),
                Telefono = entity.Telefono,
                Email = entity.Email,
                FechaNacimiento = entity.FechaNacimiento
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PacienteViewModel model)
        {
            var cedulaNorm = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError(nameof(model.Cedula), "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View(model);
            }
            model.Cedula = cedulaNorm;

            if (!ModelState.IsValid)
                return View(model);

            var entity = await _pacientesRepo.GetByIdAsync(model.Id);
            if (entity == null) return NotFound();

            await IdentidadDuplicadaValidation.ValidarCedulaUnicaAsync(
                _pacientesRepo, _usuariosRepo, model.Cedula, ModelState, nameof(model.Cedula), excludePacienteId: model.Id);
            await IdentidadDuplicadaValidation.ValidarCorreoUnicoAsync(
                _pacientesRepo, _usuariosRepo, model.Email, ModelState, nameof(model.Email), excludePacienteId: model.Id);
            if (!ModelState.IsValid)
                return View(model);

            entity.Nombres = model.Nombres;
            entity.Apellidos = model.Apellidos;
            entity.Cedula = cedulaNorm;
            entity.Telefono = model.Telefono;
            entity.Email = model.Email;

            if (!string.IsNullOrWhiteSpace(model.Contrasena))
            {
                entity.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Contrasena);
            }

            entity.FechaNacimiento = model.FechaNacimiento;

            await _pacientesRepo.UpdateAsync(entity);

            TempData["Success"] = "Paciente actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desbloquear(int id)
        {
            var entity = await _pacientesRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.IntentosFallidosLogin = 0;
            entity.BloqueadoHastaUtc = null;
            entity.BloqueadoPermanentemente = false;
            await _pacientesRepo.UpdateAsync(entity);

            TempData["Success"] = "Paciente desbloqueado correctamente.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBloqueo(int id)
        {
            var entity = await _pacientesRepo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            // Toggle: apagado = desbloqueado, encendido = bloqueado
            var ahoraBloqueado = !(entity.BloqueadoPermanentemente || (entity.BloqueadoHastaUtc.HasValue && entity.BloqueadoHastaUtc.Value > DateTime.UtcNow));
            if (ahoraBloqueado)
            {
                entity.BloqueadoPermanentemente = true;
                entity.BloqueadoHastaUtc = null;
            }
            else
            {
                entity.IntentosFallidosLogin = 0;
                entity.BloqueadoHastaUtc = null;
                entity.BloqueadoPermanentemente = false;
            }

            await _pacientesRepo.UpdateAsync(entity);
            TempData["Success"] = ahoraBloqueado ? "Paciente bloqueado." : "Paciente desbloqueado.";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }
}