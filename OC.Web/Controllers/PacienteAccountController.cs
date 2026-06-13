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

namespace OC.Web.Controllers
{
    [AllowAnonymous]
    public class PacienteAccountController : Controller
    {
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly ITotpService _totpService;

        public PacienteAccountController(
            IGenericRepository<Paciente> pacientesRepo,
            ITotpService totpService)
        {
            _pacientesRepo = pacientesRepo;
            _totpService = totpService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string cedula, string contrasena)
        {
            var cedulaNorm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError("", "La cédula debe tener el formato X-XXXX-XXXX. Ejemplo: 1-2345-6789");
                return View();
            }
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                ModelState.AddModelError("", "Debe ingresar su contraseña.");
                return View();
            }

            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm
            );
            var paciente = pacientes.Items.FirstOrDefault();

            if (paciente?.BloqueadoPermanentemente == true)
            {
                ViewBag.IsLockedPermanent = true;
                ModelState.AddModelError("", "Cuenta bloqueada. Contacte al administrador para desbloquearla y asignar una nueva contraseña. Si necesita ayuda, llame al 2222-3333.");
                return View();
            }

            // WEB-HU-028 Escenario 3: bloqueo temporal por intentos fallidos
            var nowUtc = DateTime.UtcNow;
            if (paciente?.BloqueadoHastaUtc.HasValue == true && paciente.BloqueadoHastaUtc.Value > nowUtc)
            {
                var restante = paciente.BloqueadoHastaUtc.Value - nowUtc;
                var segundos = Math.Max(0, (int)Math.Ceiling(restante.TotalSeconds));
                ViewBag.LockoutSeconds = segundos;
                ModelState.AddModelError("", "Usuario bloqueado temporalmente. Espere a que finalice el tiempo de bloqueo.");
                return View();
            }

            var credencialesOk = paciente != null
                && !string.IsNullOrEmpty(paciente.Contrasena)
                && BCrypt.Net.BCrypt.Verify(contrasena, paciente.Contrasena);

            if (!credencialesOk)
            {
                if (paciente != null)
                {
                    paciente.IntentosFallidosLogin++;
                    if (paciente.IntentosFallidosLogin >= 3)
                    {
                        // 3er fallo: 5 min, 4to: 10, 5to: 15, ... hasta 25 (máx). Luego bloqueo permanente.
                        var minutosBloqueo = Math.Min(25, (paciente.IntentosFallidosLogin - 2) * 5);
                        paciente.BloqueadoHastaUtc = nowUtc.AddMinutes(minutosBloqueo);

                        if (paciente.IntentosFallidosLogin >= 5)
                        {
                            paciente.BloqueadoPermanentemente = true;
                            paciente.BloqueadoHastaUtc = null;
                        }
                        await _pacientesRepo.UpdateAsync(paciente);

                        if (paciente.BloqueadoPermanentemente)
                        {
                            ViewBag.IsLockedPermanent = true;
                            ModelState.AddModelError("", "Cuenta bloqueada. Contacte al administrador para desbloquearla y asignar una nueva contraseña. Si necesita ayuda, llame al 2222-3333.");
                            return View();
                        }

                        ViewBag.LockoutSeconds = minutosBloqueo * 60;
                        var mensaje = $"Usuario bloqueado. Tiempo de espera: {minutosBloqueo} minuto(s).";
                        if (paciente.IntentosFallidosLogin >= 5)
                            mensaje += " Si necesita ayuda, llame al 2222-3333.";
                        ModelState.AddModelError("", mensaje);
                        return View();
                    }

                    await _pacientesRepo.UpdateAsync(paciente);
                    ViewBag.AttemptsLeft = 3 - paciente.IntentosFallidosLogin;
                }

                // WEB-HU-028 Escenario 2: credenciales incorrectas
                ModelState.AddModelError("", ViewBag.AttemptsLeft != null
                    ? $"Credenciales incorrectas. Tiene {ViewBag.AttemptsLeft} intento(s) más."
                    : "Credenciales incorrectas.");
                return View();
            }

            if (paciente!.IntentosFallidosLogin != 0 || paciente.BloqueadoHastaUtc.HasValue || paciente.BloqueadoPermanentemente)
            {
                paciente.IntentosFallidosLogin = 0;
                paciente.BloqueadoHastaUtc = null;
                paciente.BloqueadoPermanentemente = false;
                await _pacientesRepo.UpdateAsync(paciente);
            }

            if (paciente.TotpHabilitado && !string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                HttpContext.Session.SetInt32(AuthSessionKeys.PacientePendingId, paciente.Id);
                HttpContext.Session.SetString(AuthSessionKeys.PacientePendingRememberMe, "False");
                return RedirectToAction("VerificarTotpPaciente", "Account");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, paciente.NombreCompleto),
                new Claim(ClaimTypes.Role, "Paciente"),
                new Claim("PacienteId", paciente.Id.ToString()),
                new Claim("Cedula", paciente.Cedula)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = false });

            return RedirectToAction("Index", "Landing");
        }

        [HttpGet]
        public IActionResult RecuperarContrasena() => View(new RecuperarContrasenaTotpViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaTotpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cedulaNorm = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError(string.Empty, "Datos incorrectos.");
                return View(model);
            }

            var correoNorm = (model.Correo ?? string.Empty).Trim().ToLowerInvariant();
            var pacientes = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.Cedula == cedulaNorm && p.Email != null && p.Email.ToLower() == correoNorm
            );
            var paciente = pacientes.Items.FirstOrDefault();
            if (paciente == null || !paciente.TotpHabilitado || string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                ModelState.AddModelError(string.Empty, "Datos incorrectos o cuenta sin autenticador configurado.");
                return View(model);
            }

            // Ventana de recuperación de 15 minutos.
            var token = Guid.NewGuid().ToString("N");
            paciente.TokenRecuperacion = token;
            paciente.FechaExpiracionToken = DateTime.UtcNow.AddMinutes(15);

            if (string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                var secret = _totpService.GenerateSecretBase32();
                paciente.TotpSecretProtegido = _totpService.ProtectSecret(secret);
                paciente.TotpHabilitado = true;
                paciente.TotpConfiguradoEnUtc = DateTime.UtcNow;
            }

            await _pacientesRepo.UpdateAsync(paciente);
            return RedirectToAction(nameof(ValidarTotpRecuperacion), new { token });
        }

        [HttpGet]
        public async Task<IActionResult> ValidarTotpRecuperacion(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(Login));

            var paciente = await BuscarPacientePorTokenAsync(token);
            if (paciente == null)
            {
                TempData["Error"] = "La sesión de recuperación expiró. Inicie nuevamente.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }
            if (string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                TempData["Error"] = "No se pudo iniciar TOTP. Intente nuevamente.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            ViewBag.Token = token;
            ViewBag.Correo = paciente.Email;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(_totpService.UnprotectSecret(paciente.TotpSecretProtegido!));
            return View(new ValidarTotpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarTotpRecuperacion(string token, ValidarTotpViewModel model)
        {
            var paciente = await BuscarPacientePorTokenAsync(token);
            if (paciente == null)
            {
                TempData["Error"] = "La sesión de recuperación expiró. Inicie nuevamente.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }
            if (string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                TempData["Error"] = "No se pudo iniciar TOTP. Intente nuevamente.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            ViewBag.Token = token;
            ViewBag.Correo = paciente.Email;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(_totpService.UnprotectSecret(paciente.TotpSecretProtegido!));

            if (!ModelState.IsValid)
                return View(model);

            var secret = _totpService.UnprotectSecret(paciente.TotpSecretProtegido!);
            var valid = _totpService.VerifyCode(secret, model.Codigo);
            if (!valid)
            {
                ModelState.AddModelError(string.Empty, "Código inválido o expirado.");
                return View(model);
            }

            return RedirectToAction(nameof(RestablecerContrasena), new { token });
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));
            var paciente = await BuscarPacientePorTokenAsync(token);
            if (paciente == null)
            {
                TempData["Error"] = "El enlace ha expirado o no es válido. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }
            ViewBag.Token = token;
            return View(new RestablecerContrasenaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(string? token, RestablecerContrasenaViewModel model)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));
            ViewBag.Token = token;

            var paciente = await BuscarPacientePorTokenAsync(token);
            if (paciente == null)
            {
                TempData["Error"] = "El enlace ha expirado o no es válido. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            if (!ModelState.IsValid)
                return View(model);

            paciente.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.NuevaContrasena);
            paciente.TokenRecuperacion = null;
            paciente.FechaExpiracionToken = null;
            paciente.IntentosFallidosLogin = 0;
            paciente.BloqueadoHastaUtc = null;
            paciente.BloqueadoPermanentemente = false;
            await _pacientesRepo.UpdateAsync(paciente);

            TempData["Success"] = "Contraseña actualizada correctamente. Ya puede iniciar sesión.";
            return RedirectToAction(nameof(Login), "Account");
        }

        private async Task<Paciente?> BuscarPacientePorTokenAsync(string token)
        {
            var result = await _pacientesRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: p => p.TokenRecuperacion == token && p.FechaExpiracionToken.HasValue && p.FechaExpiracionToken.Value > DateTime.UtcNow
            );
            return result.Items.FirstOrDefault();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Landing");
        }
    }
}
