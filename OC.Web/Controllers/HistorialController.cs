using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;

namespace OC.Web.Controllers
{
    public class HistorialController : Controller
    {
        private readonly IGenericRepository<Cita> _citasRepo;

        public HistorialController(IGenericRepository<Cita> citasRepo)
        {
            _citasRepo = citasRepo;
        }

        [Authorize(Roles = "Optometrista,Admin")]
        public async Task<IActionResult> HistorialPaciente(int pacienteId)
        {
            var citas = await _citasRepo.GetPagedAsync(
                1, 100,
                filter: c => c.PacienteId == pacienteId && c.Estado == EstadoCita.Atendida,
                orderBy: q => q.OrderByDescending(c => c.FechaCreacion),
                includeProperties: "Paciente,Expediente"
            );
            return View(citas.Items);
        }
    }
}