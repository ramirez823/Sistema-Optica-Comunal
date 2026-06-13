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
    public class AccountController : Controller
    {
        private readonly IGenericRepository<Usuario> _userRepository;
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly ITotpService _totpService;

        public AccountController(
            IGenericRepository<Usuario> userRepository,
            IGenericRepository<Paciente> pacientesRepo,
            ITotpService totpService)
        {
            _userRepository = userRepository;
            _pacientesRepo = pacientesRepo;
            _totpService = totpService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var users = await _userRepository.GetPagedAsync(1, 1, u => u.Correo == model.Email.Trim(), includeProperties: "Rol");
            var user = users.Items.FirstOrDefault();

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Contrasena) && user.Activo)
            {
                return await ProcesarLoginTrabajadorAsync(user, model.RememberMe);
            }

            try
            {
                var pacientes = await _pacientesRepo.GetPagedAsync(1, 1, p => p.Email == model.Email.Trim());
                var paciente = pacientes.Items.FirstOrDefault();
                var resultadoPaciente = await ProcesarLoginPacienteAsync(paciente, model.Password, model.RememberMe);
                if (resultadoPaciente != null)
                    return resultadoPaciente;
            }
            catch
            {
                // Tabla Pacientes no disponible
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas o cuenta desactivada.");
            return View(model);
        }

        [HttpGet]
        public IActionResult VerificarTotpStaff()
        {
            if (!TryGetStaffPendingId(out _))
                return RedirectToAction(nameof(Login));
            ViewBag.TotpPostAction = nameof(VerificarTotpStaff);
            return View("VerificarTotp", new ValidarTotpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificarTotpStaff(ValidarTotpViewModel model)
        {
            if (!TryGetStaffPendingId(out var userId))
                return RedirectToAction(nameof(Login));

            var user = await ObtenerUsuarioStaffAsync(userId);
            if (user == null || !user.Activo || !user.TotpHabilitado || string.IsNullOrWhiteSpace(user.TotpSecretProtegido))
            {
                LimpiarSesionStaff();
                TempData["Error"] = "Debe configurar el autenticador antes de ingresar. Use su contraseña temporal si es la primera vez.";
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TotpPostAction = nameof(VerificarTotpStaff);
                return View("VerificarTotp", model);
            }

            var secret = _totpService.UnprotectSecret(user.TotpSecretProtegido, forStaff: true);
            if (!_totpService.VerifyCode(secret, model.Codigo))
            {
                ModelState.AddModelError(string.Empty, "Código inválido o expirado. Use el código actual de Microsoft Authenticator.");
                ViewBag.TotpPostAction = nameof(VerificarTotpStaff);
                return View("VerificarTotp", model);
            }

            var rememberMe = HttpContext.Session.GetString(AuthSessionKeys.StaffPendingRememberMe) == "True";
            LimpiarSesionStaff();
            await SignInStaffAsync(user, rememberMe);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult CambiarContrasenaObligatoria()
        {
            if (!TryGetStaffPendingId(out _))
                return RedirectToAction(nameof(Login));
            return View(new CambiarContrasenaObligatoriaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasenaObligatoria(CambiarContrasenaObligatoriaViewModel model)
        {
            if (!TryGetStaffPendingId(out var userId))
                return RedirectToAction(nameof(Login));

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.Activo)
            {
                LimpiarSesionStaff();
                return RedirectToAction(nameof(Login));
            }

            user.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.NuevaContrasena);
            user.DebeCambiarContrasena = false;
            await _userRepository.UpdateAsync(user);

            HttpContext.Session.Remove(AuthSessionKeys.StaffTotpSetupSecret);

            TempData["Success"] = "Contraseña actualizada. Configure su autenticador (solo esta vez) para continuar.";
            return RedirectToAction(nameof(ConfigurarTotpStaff));
        }

        [HttpGet]
        public async Task<IActionResult> ConfigurarTotpStaff()
        {
            if (!TryGetStaffPendingId(out var userId))
                return RedirectToAction(nameof(Login));

            var user = await ObtenerUsuarioStaffAsync(userId);
            if (user == null || !user.Activo) return RedirectToAction(nameof(Login));

            // Ya configurado en este u otro intento: entrar al sistema sin volver a escanear QR.
            if (user.TotpHabilitado && !string.IsNullOrWhiteSpace(user.TotpSecretProtegido))
            {
                var rememberMe = HttpContext.Session.GetString(AuthSessionKeys.StaffPendingRememberMe) == "True";
                LimpiarSesionStaff();
                await SignInStaffAsync(user, rememberMe);
                TempData["Success"] = "Bienvenido. Su autenticador ya estaba configurado.";
                return RedirectToAction("Index", "Home");
            }

            var secret = HttpContext.Session.GetString(AuthSessionKeys.StaffTotpSetupSecret);
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = _totpService.GenerateSecretBase32();
                HttpContext.Session.SetString(AuthSessionKeys.StaffTotpSetupSecret, secret);
            }

            PrepararViewBagTotp(secret, user.Correo);
            return View("ConfigurarTotpStaff", new ValidarTotpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarTotpStaff(ValidarTotpViewModel model)
        {
            if (!TryGetStaffPendingId(out var userId))
                return RedirectToAction(nameof(Login));

            var secret = HttpContext.Session.GetString(AuthSessionKeys.StaffTotpSetupSecret);
            if (string.IsNullOrWhiteSpace(secret))
                return RedirectToAction(nameof(ConfigurarTotpStaff));

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.Activo)
            {
                LimpiarSesionStaff();
                return RedirectToAction(nameof(Login));
            }

            PrepararViewBagTotp(secret, user.Correo);

            if (!ModelState.IsValid)
                return View("ConfigurarTotpStaff", model);

            if (!_totpService.VerifyCode(secret, model.Codigo))
            {
                ModelState.AddModelError(string.Empty, "Código TOTP inválido o expirado. Use el código actual de la app.");
                return View("ConfigurarTotpStaff", model);
            }

            user.TotpSecretProtegido = _totpService.ProtectSecret(secret, forStaff: true);
            user.TotpHabilitado = true;
            user.TotpConfiguradoEnUtc = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            HttpContext.Session.Remove(AuthSessionKeys.StaffTotpSetupSecret);

            var rememberMeOk = HttpContext.Session.GetString(AuthSessionKeys.StaffPendingRememberMe) == "True";
            LimpiarSesionStaff();
            await SignInStaffAsync(user, rememberMeOk);

            TempData["Success"] = "Autenticador configurado correctamente. Bienvenido.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult VerificarTotpPaciente()
        {
            if (!TryGetPacientePendingId(out _))
                return RedirectToAction(nameof(Login));
            ViewBag.TotpPostAction = nameof(VerificarTotpPaciente);
            return View("VerificarTotp", new ValidarTotpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerificarTotpPaciente(ValidarTotpViewModel model)
        {
            if (!TryGetPacientePendingId(out var pacienteId))
                return RedirectToAction(nameof(Login));

            var paciente = await _pacientesRepo.GetByIdAsync(pacienteId);
            if (paciente == null || string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                LimpiarSesionPaciente();
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
                return View("VerificarTotp", model);

            var secret = _totpService.UnprotectSecret(paciente.TotpSecretProtegido);
            if (!_totpService.VerifyCode(secret, model.Codigo))
            {
                ModelState.AddModelError(string.Empty, "Código inválido o expirado.");
                return View("VerificarTotp", model);
            }

            var rememberMe = HttpContext.Session.GetString(AuthSessionKeys.PacientePendingRememberMe) == "True";
            LimpiarSesionPaciente();
            await SignInPacienteAsync(paciente, rememberMe);
            return RedirectToAction("Index", "Landing");
        }

        [HttpGet]
        public IActionResult RecuperarContrasena() => View(new RecuperarContrasenaStaffViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaStaffViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var correo = model.Correo.Trim();
            var users = await _userRepository.GetPagedAsync(1, 1, u => u.Correo == correo);
            var user = users.Items.FirstOrDefault();

            if (user == null || !user.TotpHabilitado || string.IsNullOrWhiteSpace(user.TotpSecretProtegido))
            {
                ModelState.AddModelError(string.Empty, "Datos incorrectos o cuenta sin autenticador configurado. Contacte al administrador.");
                return View(model);
            }

            user.TokenRecuperacion = Guid.NewGuid().ToString("N");
            user.FechaExpiracionToken = DateTime.UtcNow.AddMinutes(15);
            await _userRepository.UpdateAsync(user);

            return RedirectToAction(nameof(ValidarTotpRecuperacionStaff), new { token = user.TokenRecuperacion });
        }

        [HttpGet]
        public async Task<IActionResult> ValidarTotpRecuperacionStaff(string? token)
        {
            var user = await BuscarUsuarioPorTokenAsync(token);
            if (user == null)
            {
                TempData["Error"] = "La sesión de recuperación expiró. Inicie nuevamente.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            ViewBag.Token = token;
            ViewBag.Correo = user.Correo;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(
                _totpService.UnprotectSecret(user.TotpSecretProtegido!, forStaff: true));
            return View(new ValidarTotpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarTotpRecuperacionStaff(string token, ValidarTotpViewModel model)
        {
            var user = await BuscarUsuarioPorTokenAsync(token);
            if (user == null)
            {
                TempData["Error"] = "La sesión de recuperación expiró.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            ViewBag.Token = token;
            ViewBag.Correo = user.Correo;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(
                _totpService.UnprotectSecret(user.TotpSecretProtegido!, forStaff: true));

            if (!ModelState.IsValid)
                return View(model);

            var secret = _totpService.UnprotectSecret(user.TotpSecretProtegido!, forStaff: true);
            if (!_totpService.VerifyCode(secret, model.Codigo))
            {
                ModelState.AddModelError(string.Empty, "Código inválido o expirado.");
                return View(model);
            }

            return RedirectToAction(nameof(RestablecerContrasenaStaff), new { token });
        }

        [HttpGet]
        public async Task<IActionResult> RestablecerContrasenaStaff(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));
            var user = await BuscarUsuarioPorTokenAsync(token);
            if (user == null)
            {
                TempData["Error"] = "El enlace ha expirado. Solicite uno nuevo.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }
            ViewBag.Token = token;
            return View(new RestablecerContrasenaViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasenaStaff(string? token, RestablecerContrasenaViewModel model)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(Login));
            ViewBag.Token = token;

            var user = await BuscarUsuarioPorTokenAsync(token);
            if (user == null)
            {
                TempData["Error"] = "El enlace ha expirado.";
                return RedirectToAction(nameof(RecuperarContrasena));
            }

            if (!ModelState.IsValid)
                return View(model);

            user.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.NuevaContrasena);
            user.TokenRecuperacion = null;
            user.FechaExpiracionToken = null;
            user.DebeCambiarContrasena = false;
            await _userRepository.UpdateAsync(user);

            TempData["Success"] = "Contraseña actualizada. Ya puede iniciar sesión con su autenticador.";
            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Landing");
        }

        private async Task<IActionResult> ProcesarLoginTrabajadorAsync(Usuario user, bool rememberMe)
        {
            user = await ObtenerUsuarioStaffAsync(user.Id) ?? user;

            if (AuthSessionKeys.EsCorreoExentoTotp(user.Correo))
            {
                if (user.DebeCambiarContrasena)
                {
                    HttpContext.Session.SetInt32(AuthSessionKeys.StaffPendingUserId, user.Id);
                    HttpContext.Session.SetString(AuthSessionKeys.StaffPendingRememberMe, rememberMe.ToString());
                    return RedirectToAction(nameof(CambiarContrasenaObligatoria));
                }
                await SignInStaffAsync(user, rememberMe);
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.SetInt32(AuthSessionKeys.StaffPendingUserId, user.Id);
            HttpContext.Session.SetString(AuthSessionKeys.StaffPendingRememberMe, rememberMe.ToString());

            if (user.DebeCambiarContrasena)
                return RedirectToAction(nameof(CambiarContrasenaObligatoria));

            if (!user.TotpHabilitado || string.IsNullOrWhiteSpace(user.TotpSecretProtegido))
                return RedirectToAction(nameof(ConfigurarTotpStaff));

            return RedirectToAction(nameof(VerificarTotpStaff));
        }

        private async Task<Usuario?> ObtenerUsuarioStaffAsync(int userId)
        {
            var result = await _userRepository.GetPagedAsync(
                pageIndex: 1,
                pageSize: 1,
                filter: u => u.Id == userId,
                includeProperties: "Rol");
            return result.Items.FirstOrDefault();
        }

        private async Task<IActionResult?> ProcesarLoginPacienteAsync(Paciente? paciente, string password, bool rememberMe)
        {
            var nowUtc = DateTime.UtcNow;
            if (paciente?.BloqueadoPermanentemente == true)
            {
                ViewBag.IsLockedPermanent = true;
                ModelState.AddModelError(string.Empty, "Cuenta bloqueada. Contacte al administrador.");
                return View(new LoginViewModel());
            }

            if (paciente?.BloqueadoHastaUtc is { } bloqueo && bloqueo > nowUtc)
            {
                ViewBag.LockoutSeconds = Math.Max(0, (int)Math.Ceiling((bloqueo - nowUtc).TotalSeconds));
                ModelState.AddModelError(string.Empty, "Usuario bloqueado temporalmente.");
                return View(new LoginViewModel());
            }

            var credencialesOk = paciente != null
                && !string.IsNullOrEmpty(paciente.Contrasena)
                && BCrypt.Net.BCrypt.Verify(password, paciente.Contrasena);

            if (!credencialesOk)
            {
                if (paciente != null)
                    await AplicarFalloLoginPacienteAsync(paciente, nowUtc);
                ModelState.AddModelError(string.Empty, "Credenciales incorrectas.");
                return View(new LoginViewModel());
            }

            paciente!.IntentosFallidosLogin = 0;
            paciente.BloqueadoHastaUtc = null;
            paciente.BloqueadoPermanentemente = false;
            await _pacientesRepo.UpdateAsync(paciente);

            if (paciente.TotpHabilitado && !string.IsNullOrWhiteSpace(paciente.TotpSecretProtegido))
            {
                HttpContext.Session.SetInt32(AuthSessionKeys.PacientePendingId, paciente.Id);
                HttpContext.Session.SetString(AuthSessionKeys.PacientePendingRememberMe, rememberMe.ToString());
                return RedirectToAction(nameof(VerificarTotpPaciente));
            }

            await SignInPacienteAsync(paciente, rememberMe);
            return RedirectToAction("Index", "Landing");
        }

        private async Task AplicarFalloLoginPacienteAsync(Paciente paciente, DateTime nowUtc)
        {
            paciente.IntentosFallidosLogin++;
            if (paciente.IntentosFallidosLogin >= 3)
            {
                var minutosBloqueo = Math.Min(25, (paciente.IntentosFallidosLogin - 2) * 5);
                paciente.BloqueadoHastaUtc = nowUtc.AddMinutes(minutosBloqueo);
                if (paciente.IntentosFallidosLogin >= 5)
                {
                    paciente.BloqueadoPermanentemente = true;
                    paciente.BloqueadoHastaUtc = null;
                }
                await _pacientesRepo.UpdateAsync(paciente);
            }
            else
            {
                await _pacientesRepo.UpdateAsync(paciente);
            }
        }

        private void PrepararViewBagTotp(string secret, string labelEmail)
        {
            var issuer = "OpticaComunal";
            var label = $"{issuer}:{labelEmail}";
            var otpauth = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";
            ViewBag.QrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=230x230&data=" + Uri.EscapeDataString(otpauth);
            ViewBag.Correo = labelEmail;
            ViewBag.ManualKey = _totpService.GetManualEntryKey(secret);
        }

        private async Task SignInStaffAsync(Usuario user, bool rememberMe)
        {
            var loaded = await _userRepository.GetPagedAsync(1, 1, u => u.Id == user.Id, includeProperties: "Rol");
            var rolNombre = loaded.Items.FirstOrDefault()?.Rol.Nombre ?? "Recepcion";

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Nombre),
                new(ClaimTypes.Email, user.Correo),
                new(ClaimTypes.Role, rolNombre),
                new("UserId", user.Id.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = rememberMe });
        }

        private async Task SignInPacienteAsync(Paciente paciente, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, paciente.NombreCompleto),
                new(ClaimTypes.Email, paciente.Email ?? ""),
                new(ClaimTypes.Role, "Paciente"),
                new("PacienteId", paciente.Id.ToString()),
                new("Cedula", paciente.Cedula)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = rememberMe });
        }

        private bool TryGetStaffPendingId(out int userId)
        {
            userId = 0;
            var id = HttpContext.Session.GetInt32(AuthSessionKeys.StaffPendingUserId);
            if (!id.HasValue) return false;
            userId = id.Value;
            return true;
        }

        private bool TryGetPacientePendingId(out int pacienteId)
        {
            pacienteId = 0;
            var id = HttpContext.Session.GetInt32(AuthSessionKeys.PacientePendingId);
            if (!id.HasValue) return false;
            pacienteId = id.Value;
            return true;
        }

        private void LimpiarSesionStaff()
        {
            HttpContext.Session.Remove(AuthSessionKeys.StaffPendingUserId);
            HttpContext.Session.Remove(AuthSessionKeys.StaffPendingRememberMe);
            HttpContext.Session.Remove(AuthSessionKeys.StaffTotpSetupSecret);
        }

        private void LimpiarSesionPaciente()
        {
            HttpContext.Session.Remove(AuthSessionKeys.PacientePendingId);
            HttpContext.Session.Remove(AuthSessionKeys.PacientePendingRememberMe);
        }

        private async Task<Usuario?> BuscarUsuarioPorTokenAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var result = await _userRepository.GetPagedAsync(
                1, 1,
                u => u.TokenRecuperacion == token
                     && u.FechaExpiracionToken.HasValue
                     && u.FechaExpiracionToken.Value > DateTime.UtcNow);
            return result.Items.FirstOrDefault();
        }
    }
}
