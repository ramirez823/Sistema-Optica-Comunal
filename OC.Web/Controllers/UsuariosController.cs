using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using OC.Web.Helpers;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly IGenericRepository<Usuario> _userRepository;
        private readonly IGenericRepository<Paciente> _pacientesRepo;
        private readonly IGenericRepository<Rol> _roleRepository;
        private readonly IGenericRepository<Sucursal> _branchRepository;

        public UsuariosController(
            IGenericRepository<Usuario> userRepository,
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Rol> roleRepository,
            IGenericRepository<Sucursal> branchRepository)
        {
            _userRepository = userRepository;
            _pacientesRepo = pacientesRepo;
            _roleRepository = roleRepository;
            _branchRepository = branchRepository;
        }

        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            var pagedResult = await _userRepository.GetPagedAsync(
                pageIndex: pageIndex,
                pageSize: 10,
                orderBy: q => q.OrderBy(u => u.Nombre),
                includeProperties: "Rol,Sucursal"
            );
            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new UserViewModel();
            await LoadDropdowns(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "La contraseña temporal es obligatoria.");

            ValidarCedula(model);
            ValidarIbanYBanco(model);
            await ValidarDuplicadosAsync(model, null);
            await ValidarRolNoPacienteAsync(model);

            if (ModelState.IsValid)
            {
                var entity = new Usuario
                {
                    Nombre = model.Name,
                    Correo = model.Email.Trim(),
                    Cedula = CedulaValidation.Normalizar(model.Cedula),
                    Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Password!),
                    RolId = model.RoleId,
                    SucursalId = model.SucursalId,
                    Activo = true,
                    DebeCambiarContrasena = true,
                    TotpHabilitado = false,
                    SalarioBase = model.SalarioBase,
                    FechaContratacion = model.FechaContratacion,
                    Banco = model.Banco,
                    NumeroCuentaIBAN = IbanValidation.ConstruirIban(
                        model.IbanBloque1, model.IbanBloque2, model.IbanBloque3, model.IbanBloque4, model.IbanBloque5)
                };

                await _userRepository.AddAsync(entity);
                TempData["Success"] = "Usuario creado. Entregue la contraseña temporal; en el primer ingreso deberá cambiarla y configurar Microsoft Authenticator.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AssignRole()
        {
            var model = new RoleAssignmentViewModel();
            await LoadAssignmentDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(RoleAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetByIdAsync(model.UserId!.Value);
                if (user == null)
                    ModelState.AddModelError(nameof(model.UserId), "El usuario seleccionado no existe.");

                var role = await _roleRepository.GetByIdAsync(model.RoleId!.Value);
                if (role == null)
                    ModelState.AddModelError(nameof(model.RoleId), "El rol seleccionado no existe.");
                else if (role.Nombre.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
                    ModelState.AddModelError(nameof(model.RoleId), "No se puede asignar el rol Paciente a un trabajador del sistema.");

                if (ModelState.IsValid)
                {
                    user!.RolId = role!.Id;
                    await _userRepository.UpdateAsync(user);
                    TempData["Success"] = "Rol asignado correctamente. Los permisos fueron actualizados.";
                    return RedirectToAction(nameof(AssignRole));
                }
            }

            await LoadAssignmentDropdowns(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _userRepository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            var model = MapToViewModel(entity);
            await LoadDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserViewModel model)
        {
            ValidarCedula(model);
            ValidarIbanYBanco(model);
            await ValidarDuplicadosAsync(model, model.Id);
            await ValidarRolNoPacienteAsync(model);

            if (ModelState.IsValid)
            {
                var entity = await _userRepository.GetByIdAsync(model.Id);
                if (entity == null) return NotFound();

                entity.Nombre = model.Name;
                entity.Correo = model.Email.Trim();
                entity.Cedula = CedulaValidation.Normalizar(model.Cedula);
                entity.RolId = model.RoleId;
                entity.SucursalId = model.SucursalId;
                entity.SalarioBase = model.SalarioBase;
                entity.FechaContratacion = model.FechaContratacion;
                entity.Banco = model.Banco;
                entity.NumeroCuentaIBAN = IbanValidation.ConstruirIban(
                    model.IbanBloque1, model.IbanBloque2, model.IbanBloque3, model.IbanBloque4, model.IbanBloque5);

                if (!string.IsNullOrEmpty(model.Password))
                {
                    entity.Contrasena = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    entity.DebeCambiarContrasena = true;
                }

                await _userRepository.UpdateAsync(entity);
                TempData["Success"] = "Usuario actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var entity = await _userRepository.GetByIdAsync(id);
            if (entity == null) return NotFound();

            entity.Activo = !entity.Activo;
            await _userRepository.UpdateAsync(entity);
            return RedirectToAction(nameof(Index));
        }

        private static UserViewModel MapToViewModel(Usuario entity)
        {
            var model = new UserViewModel
            {
                Id = entity.Id,
                Name = entity.Nombre,
                Email = entity.Correo,
                Cedula = CedulaValidation.FormatearParaMostrar(entity.Cedula),
                RoleId = entity.RolId,
                SucursalId = entity.SucursalId,
                SalarioBase = entity.SalarioBase,
                FechaContratacion = entity.FechaContratacion,
                Banco = entity.Banco,
                NumeroCuentaIBAN = entity.NumeroCuentaIBAN
            };
            IbanValidation.DescomponerParaFormulario(entity.NumeroCuentaIBAN,
                out var b1, out var b2, out var b3, out var b4, out var b5);
            model.IbanBloque1 = b1;
            model.IbanBloque2 = b2;
            model.IbanBloque3 = b3;
            model.IbanBloque4 = b4;
            model.IbanBloque5 = b5;
            return model;
        }

        private void ValidarCedula(UserViewModel model)
        {
            var cedulaNorm = CedulaValidation.Normalizar(model.Cedula);
            if (!CedulaValidation.EsFormatoValido(cedulaNorm))
            {
                ModelState.AddModelError(nameof(model.Cedula),
                    $"La cédula debe tener el formato {CedulaValidation.EjemploFormato}.");
                return;
            }
            model.Cedula = CedulaValidation.FormatearParaMostrar(cedulaNorm);
        }

        private void ValidarIbanYBanco(UserViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Banco))
                ModelState.AddModelError(nameof(model.Banco), "Debe seleccionar un banco.");

            var iban = IbanValidation.ConstruirIban(
                model.IbanBloque1, model.IbanBloque2, model.IbanBloque3, model.IbanBloque4, model.IbanBloque5);
            if (!IbanValidation.EsIbanValido(iban))
                ModelState.AddModelError(nameof(model.IbanBloque1), "El IBAN debe tener 20 dígitos después de CR (4 grupos de 4).");
        }

        private async Task ValidarDuplicadosAsync(UserViewModel model, int? excludeId)
        {
            await IdentidadDuplicadaValidation.ValidarCedulaUnicaAsync(
                _pacientesRepo, _userRepository, model.Cedula, ModelState, nameof(model.Cedula),
                excludeUsuarioId: excludeId);
            await IdentidadDuplicadaValidation.ValidarCorreoUnicoAsync(
                _pacientesRepo, _userRepository, model.Email, ModelState, nameof(model.Email),
                excludeUsuarioId: excludeId);
        }

        private async Task ValidarRolNoPacienteAsync(UserViewModel model)
        {
            var rol = await _roleRepository.GetByIdAsync(model.RoleId);
            if (rol != null && rol.Nombre.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.RoleId), "No puede crear trabajadores con rol Paciente. Use el módulo de Pacientes.");
        }

        private async Task LoadDropdowns(UserViewModel model)
        {
            model.RolesList = await ObtenerRoles();
            model.SucursalesList = await ObtenerSucursales();
            model.BancosList = CostaRicaBancos.Nombres.Select(b => new SelectListItem
            {
                Text = b,
                Value = b,
                Selected = b == model.Banco
            });
        }

        private async Task LoadAssignmentDropdowns(RoleAssignmentViewModel model)
        {
            var roles = await _roleRepository.GetPagedAsync(1, 100);
            var users = await _userRepository.GetPagedAsync(1, 100, orderBy: q => q.OrderBy(u => u.Nombre));

            model.RolesList = roles.Items
                .Where(r => !r.Nombre.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
                .Select(x => new SelectListItem { Text = x.Nombre, Value = x.Id.ToString() });
            model.UsersList = users.Items.Select(x => new SelectListItem { Text = $"{x.Nombre} ({x.Correo})", Value = x.Id.ToString() });
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerRoles(int? seleccionado = null)
        {
            var roles = await _roleRepository.GetPagedAsync(1, 100, orderBy: q => q.OrderBy(r => r.Nombre));
            return roles.Items
                .Where(r => !r.Nombre.Equals("Paciente", StringComparison.OrdinalIgnoreCase))
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Nombre,
                    Selected = seleccionado.HasValue && r.Id == seleccionado.Value
                });
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerSucursales(int? seleccionado = null)
        {
            var sucursales = await _branchRepository.GetPagedAsync(1, 100, filter: s => s.Activo, orderBy: q => q.OrderBy(s => s.Nombre));
            return sucursales.Items.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Nombre,
                Selected = seleccionado.HasValue && s.Id == seleccionado.Value
            });
        }
    }
}
