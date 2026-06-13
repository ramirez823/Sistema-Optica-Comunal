using Microsoft.AspNetCore.Mvc.ModelBinding;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;

namespace OC.Web.Helpers
{
    /// <summary>Evita cédula o correo duplicados entre Pacientes y Usuarios (trabajadores).</summary>
    public static class IdentidadDuplicadaValidation
    {
        public static async Task ValidarCedulaUnicaAsync(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Usuario> usuariosRepo,
            string cedula,
            ModelStateDictionary modelState,
            string fieldName = "Cedula",
            int? excludePacienteId = null,
            int? excludeUsuarioId = null)
        {
            var norm = CedulaValidation.Normalizar(cedula);
            if (!CedulaValidation.EsFormatoValido(norm))
                return;

            var enPacientes = (await pacientesRepo.GetPagedAsync(1, 1,
                p => p.Cedula == norm && (!excludePacienteId.HasValue || p.Id != excludePacienteId.Value))).Items.Any();
            if (enPacientes)
            {
                modelState.AddModelError(fieldName, "El número de cédula ya está registrado en el sistema.");
                return;
            }

            var enUsuarios = (await usuariosRepo.GetPagedAsync(1, 1,
                u => u.Cedula == norm && (!excludeUsuarioId.HasValue || u.Id != excludeUsuarioId.Value))).Items.Any();
            if (enUsuarios)
                modelState.AddModelError(fieldName, "El número de cédula ya está registrado como trabajador del sistema.");
        }

        public static async Task ValidarCorreoUnicoAsync(
            IGenericRepository<Paciente> pacientesRepo,
            IGenericRepository<Usuario> usuariosRepo,
            string correo,
            ModelStateDictionary modelState,
            string fieldName = "Email",
            int? excludePacienteId = null,
            int? excludeUsuarioId = null)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return;

            var correoNorm = correo.Trim().ToLowerInvariant();

            var enPacientes = (await pacientesRepo.GetPagedAsync(1, 1,
                p => p.Email != null && p.Email.ToLower() == correoNorm
                     && (!excludePacienteId.HasValue || p.Id != excludePacienteId.Value))).Items.Any();
            if (enPacientes)
            {
                modelState.AddModelError(fieldName, "El correo electrónico ya está registrado en el sistema.");
                return;
            }

            var enUsuarios = (await usuariosRepo.GetPagedAsync(1, 1,
                u => u.Correo.ToLower() == correoNorm
                     && (!excludeUsuarioId.HasValue || u.Id != excludeUsuarioId.Value))).Items.Any();
            if (enUsuarios)
                modelState.AddModelError(fieldName, "El correo electrónico ya está registrado como trabajador del sistema.");
        }
    }
}
