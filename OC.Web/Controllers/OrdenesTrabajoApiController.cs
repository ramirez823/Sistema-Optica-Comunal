using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Recepcion,Optometrista,TecnicoOcular")]
    [Route("api/ordenes-trabajo")]
    public class OrdenesTrabajoApiController : ControllerBase
    {
        private readonly IGenericRepository<Venta> _ventasRepo;

        public OrdenesTrabajoApiController(IGenericRepository<Venta> ventasRepo)
        {
            _ventasRepo = ventasRepo;
        }

        [HttpGet("ventas-por-paciente/{pacienteId}")]
        public async Task<IActionResult> GetVentasPorPaciente(int pacienteId)
        {
            if (pacienteId <= 0)
                return BadRequest();

            var result = await _ventasRepo.GetPagedAsync(
                pageIndex: 1,
                pageSize: 100,
                filter: v => v.PacienteId == pacienteId,
                orderBy: q => q.OrderByDescending(v => v.FechaVenta),
                includeProperties: "Paciente");

            var ventas = result.Items.Select(v => new VentaPorPacienteDto
            {
                Id = v.Id,
                NumeroFactura = v.NumeroFactura,
                FechaVenta = v.FechaVenta,
                Total = v.Total
            });

            return Ok(ventas);
        }
    }
}