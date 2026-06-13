using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Web.ViewModels;

namespace OC.Web.Controllers
{
    [Authorize(Roles = "Admin,Optometrista,Recepcion")]
    public class ExpedientessController : Controller
    {
        private readonly IGenericRepository<Expediente> _expedienteRepo;
        private readonly IGenericRepository<Cita> _citaRepo;
        private readonly IGenericRepository<ValorClinico> _valorClinicoRepo;
        private readonly IGenericRepository<DocumentoExpediente> _documentoRepo;
        private readonly IValorClinicoRepository _valorClinicoReadRepo;
        private readonly ILogger<ExpedientessController> _logger;

        public ExpedientessController(
            IGenericRepository<Expediente> expedienteRepo,
            IGenericRepository<Cita> citaRepo,
            IGenericRepository<ValorClinico> valorClinicoRepo,
            IGenericRepository<DocumentoExpediente> documentoRepo,
            IValorClinicoRepository valorClinicoReadRepo,
            ILogger<ExpedientessController> logger)
        {
            _expedienteRepo = expedienteRepo;
            _citaRepo = citaRepo;
            _valorClinicoRepo = valorClinicoRepo;
            _documentoRepo = documentoRepo;
            _valorClinicoReadRepo = valorClinicoReadRepo;
            _logger = logger;
        }

        // GET: Expedientess/Create?citaId=5
        [HttpGet]
        public async Task<IActionResult> Create(int citaId)
        {
            var cita = (await _citaRepo.GetPagedAsync(
                1, 1,
                filter: c => c.Id == citaId,
                includeProperties: "Paciente,Expediente"
            )).Items.FirstOrDefault();

            if (cita == null)
                return NotFound();

            if (cita.Estado != EstadoCita.Confirmada && cita.Estado != EstadoCita.Atendida)
            {
                TempData["Error"] = "Solo se puede crear expediente para citas en estado Confirmada o Atendida.";
                return RedirectToAction("HistorialPaciente", "CitasPublicas", new { pacienteId = cita.PacienteId });
            }

            if (cita.Expediente != null)
            {
                TempData["Error"] = "Esta cita ya tiene un expediente registrado.";
                return RedirectToAction("Details", new { id = cita.Expediente.Id });
            }

            var viewModel = new ExpedienteCreateViewModel
            {
                CitaId = cita.Id,
                NombrePaciente = cita.Paciente.NombreCompleto,
                FechaCita = cita.FechaHora,
                PacienteId = cita.PacienteId,
                CitaAtendida = (cita.Estado == EstadoCita.Atendida)
            };

            return View("~/Views/Expedientess/Create.cshtml", viewModel);
        }

        // POST: Expedientess/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpedienteCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var cita = (await _citaRepo.GetPagedAsync(
                    1, 1,
                    filter: c => c.Id == model.CitaId,
                    includeProperties: "Paciente"
                )).Items.FirstOrDefault();

                if (cita != null)
                {
                    model.NombrePaciente = cita.Paciente.NombreCompleto;
                    model.FechaCita = cita.FechaHora;
                    model.PacienteId = cita.PacienteId;
                }

                return View("~/Views/Expedientess/Create.cshtml", model);
            }

            var citaParaActualizar = (await _citaRepo.GetPagedAsync(
                1, 1,
                filter: c => c.Id == model.CitaId,
                includeProperties: "Expediente"
            )).Items.FirstOrDefault();

            if (citaParaActualizar == null)
                return NotFound();

            if (citaParaActualizar.Estado != EstadoCita.Confirmada && citaParaActualizar.Estado != EstadoCita.Atendida)
            {
                TempData["Error"] = "La cita ya no está en estado válido.";
                return RedirectToAction("HistorialPaciente", "CitasPublicas", new { pacienteId = citaParaActualizar.PacienteId });
            }

            if (citaParaActualizar.Expediente != null)
            {
                TempData["Error"] = "La cita ya tiene un expediente.";
                return RedirectToAction("Details", new { id = citaParaActualizar.Expediente.Id });
            }

            var expediente = new Expediente
            {
                CitaId = model.CitaId,
                MotivoConsulta = model.MotivoConsulta,
                Observaciones = model.Observaciones,
                FechaRegistro = DateTime.Now
            };

            await _expedienteRepo.AddAsync(expediente);

            // Si la cita aún está programada, la marcamos como atendida
            if (citaParaActualizar.Estado == EstadoCita.Confirmada)
            {
                citaParaActualizar.Estado = EstadoCita.Atendida;
                citaParaActualizar.ObservacionesEspecialista = model.MotivoConsulta;
                await _citaRepo.UpdateAsync(citaParaActualizar);
            }

            TempData["Success"] = "Expediente registrado correctamente.";
            return RedirectToAction(nameof(Details), new { id = expediente.Id });
        }

        // GET: Expedientess/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // El repositorio específico ordena las colecciones hijas en SQL
            // (ValoresClinicos y Documentos), evitando el OrderByDescending en la vista.
            var expediente = await _valorClinicoReadRepo.GetExpedienteConValoresOrdenadosAsync(id);

            if (expediente == null)
                return NotFound();

            return View("~/Views/Expedientess/Details.cshtml", expediente);
        }

        // GET: Expedientess/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var expediente = (await _expedienteRepo.GetPagedAsync(
                1, 1,
                filter: e => e.Id == id,
                includeProperties: "Cita.Paciente"
            )).Items.FirstOrDefault();

            if (expediente == null)
                return NotFound();

            var viewModel = new ExpedienteEditViewModel
            {
                Id = expediente.Id,
                MotivoConsulta = expediente.MotivoConsulta,
                Observaciones = expediente.Observaciones,
                NombrePaciente = expediente.Cita?.Paciente?.NombreCompleto,
                FechaCita = expediente.Cita?.FechaHora ?? DateTime.Now,
                PacienteId = expediente.Cita?.PacienteId ?? 0,
                CitaAtendida = (expediente.Cita?.Estado == EstadoCita.Atendida)
            };

            return View("~/Views/Expedientess/Edit.cshtml", viewModel);
        }

        // POST: Expedientess/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExpedienteEditViewModel model)

        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Expedientess/Edit.cshtml", model);
            }

            var expediente = await _expedienteRepo.GetByIdAsync(model.Id);
            if (expediente == null)
                return NotFound();

            expediente.MotivoConsulta = model.MotivoConsulta;
            expediente.Observaciones = model.Observaciones;

            await _expedienteRepo.UpdateAsync(expediente);

            TempData["Success"] = "Expediente actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = expediente.Id });
        }


        // GET: Expedientess/AddValorClinico/5
        [HttpGet]
        public async Task<IActionResult> AddValorClinico(int expedienteId)
        {
            var expediente = (await _expedienteRepo.GetPagedAsync(
                1, 1,
                filter: e => e.Id == expedienteId,
                includeProperties: "Cita.Paciente"
            )).Items.FirstOrDefault();

            if (expediente == null)
                return NotFound();

            var viewModel = new ValorClinicoViewModel
            {
                ExpedienteId = expediente.Id,
                NombrePaciente = expediente.Cita?.Paciente?.NombreCompleto,
                MotivoConsulta = expediente.MotivoConsulta
            };

            return View("~/Views/Expedientess/AddValorClinico.cshtml", viewModel);
        }

        // POST: Expedientess/AddValorClinico
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddValorClinico(ValorClinicoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar datos del expediente para la vista
                var expediente = (await _expedienteRepo.GetPagedAsync(
                    1, 1,
                    filter: e => e.Id == model.ExpedienteId,
                    includeProperties: "Cita.Paciente"
                )).Items.FirstOrDefault();

                if (expediente != null)
                {
                    model.NombrePaciente = expediente.Cita?.Paciente?.NombreCompleto;
                    model.MotivoConsulta = expediente.MotivoConsulta;
                }

                return View("~/Views/Expedientess/AddValorClinico.cshtml", model);
            }

            // Validar que el Expediente exista antes de agregar un valor clínico.
            // Esto evita inserciones huérfanas si se manipula el id por URL.
            var expedienteValido = await _expedienteRepo.GetByIdAsync(model.ExpedienteId);
            if (expedienteValido == null)
            {
                TempData["Error"] = "El expediente indicado no existe. No se puede agregar el valor clínico.";
                return RedirectToAction(nameof(Index), "CitasPublicas");
            }

            var valorClinico = new ValorClinico
            {
                ExpedienteId = model.ExpedienteId,
                Diagnostico = model.Diagnostico,
                // Refracción
                EsferaOD = model.EsferaOD,
                CilindroOD = model.CilindroOD,
                EjeOD = model.EjeOD,
                EsferaOI = model.EsferaOI,
                CilindroOI = model.CilindroOI,
                EjeOI = model.EjeOI,
                ADD_Od = model.ADD_Od,
                ADD_Oi = model.ADD_Oi,
                // Agudeza visual
                AvOdLejos = model.AvOdLejos,
                AvOiLejos = model.AvOiLejos,
                AvOdCerca = model.AvOdCerca,
                AvOiCerca = model.AvOiCerca,
                // Presión intraocular
                PioOd = model.PioOd,
                PioOi = model.PioOi,
                // Otros
                PercepcionColores = model.PercepcionColores,
                MotilidadOcular = model.MotilidadOcular,
                FondoOjo = model.FondoOjo,
                CampoVisual = model.CampoVisual,
                Observaciones = model.Observaciones,
                FechaRegistro = DateTime.Now
            };

            await _valorClinicoRepo.AddAsync(valorClinico);

            TempData["Success"] = "Valor clínico agregado correctamente al historial.";
            return RedirectToAction(nameof(Details), new { id = model.ExpedienteId });
        }


        // GET: Expedientess/AddDocumento/5
        [HttpGet]
        public async Task<IActionResult> AddDocumento(int expedienteId)
        {
            try
            {
                var expediente = await _expedienteRepo.GetByIdAsync(expedienteId);
                if (expediente == null)
                    return NotFound();

                var viewModel = new DocumentoExpedienteViewModel
                {
                    ExpedienteId = expediente.Id
                };

                return View("~/Views/Expedientess/AddDocumento.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar vista AddDocumento para expediente {ExpedienteId}", expedienteId);
                TempData["Error"] = "Ocurrió un error al cargar la página.";
                return RedirectToAction("Details", new { id = expedienteId });
            }
        }

        // POST: Expedientess/AddDocumento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDocumento(DocumentoExpedienteViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View("~/Views/Expedientess/AddDocumento.cshtml", model);

                if (model.Archivo == null || model.Archivo.Length == 0)
                {
                    ModelState.AddModelError("Archivo", "El archivo no puede estar vacío.");
                    return View("~/Views/Expedientess/AddDocumento.cshtml", model);
                }

                var extension = Path.GetExtension(model.Archivo.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Archivo", "Tipo de archivo no permitido. Extensiones permitidas: .pdf, .jpg, .jpeg, .png, .doc, .docx, .xls, .xlsx");
                    return View("~/Views/Expedientess/AddDocumento.cshtml", model);
                }

                if (model.Archivo.Length > 10 * 1024 * 1024) // 10 MB
                {
                    ModelState.AddModelError("Archivo", "El archivo no puede superar los 10 MB.");
                    return View("~/Views/Expedientess/AddDocumento.cshtml", model);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documentos");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Archivo.CopyToAsync(stream);
                }

                var documento = new DocumentoExpediente
                {
                    ExpedienteId = model.ExpedienteId,
                    NombreArchivo = model.Archivo.FileName,
                    RutaArchivo = "/uploads/documentos/" + uniqueFileName,
                    FechaSubida = DateTime.Now
                };

                await _documentoRepo.AddAsync(documento);

                TempData["Success"] = "Documento adjuntado correctamente.";
                return RedirectToAction("Details", new { id = model.ExpedienteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir documento para expediente {ExpedienteId}", model?.ExpedienteId);
                ModelState.AddModelError("", "Ocurrió un error al subir el archivo. Intente nuevamente.");
                return View("~/Views/Expedientess/AddDocumento.cshtml", model);
            }
        }
    }
}