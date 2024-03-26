using Data;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.Drawing;

namespace POC_Forms.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentosController : ControllerBase
    {
        private readonly ILogger<DocumentosController> _logger;
        private readonly DataContext _context;

        public DocumentosController(ILogger<DocumentosController> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpPost(Name = "CreteDocumento")]
        public async Task<IActionResult> Post([FromForm] NovoDocumentoForm req, CancellationToken cancellationToken)
        {
            var docId = Guid.Parse("25e96b3e-ee17-42f9-9afa-ee02a8c604d3");
            var word = new WordDocument(req.Arquivo.OpenReadStream(), FormatType.Automatic);
            var documento = new Documento
            {
                Id = docId,
                Nome = req.Nome,
                Path = req.Arquivo.FileName,
                Campos = word.MailMerge.GetMergeFieldNames()
                .Distinct()
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(c => new Campo
                {
                    Id = Guid.NewGuid(),
                    Key = c,
                    DocumentoId = docId
                }).ToList()
            };

            _logger.LogInformation("Creating new document {0}", documento.Nome);
            _logger.LogInformation("Document has {0} fields", documento.Campos.Count);
            foreach (var campo in documento.Campos)
            {
                _logger.LogInformation("Field {0} added", campo.Key);
            }

            await _context.Documentos.AddAsync(documento, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", documento.Path);
            if (Directory.Exists(Path.GetDirectoryName(path)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await req.Arquivo.CopyToAsync(stream, cancellationToken);
                stream.Close();
            }

            return Ok(docId);
        }

        [HttpPost("{id}/preencher")]
        public async Task<IActionResult> Preencher(Guid id, [FromBody] Dictionary<string, string> campos, CancellationToken cancellationToken)
        {
            var documento = await _context.Documentos
                .Include(d => d.Campos)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (documento == null)
            {
                return NotFound();
            }

            foreach (var campo in documento.Campos)
            {
                if (campos.TryGetValue(campo.Key, out var value))
                {
                    _logger.LogInformation("Updating field {0} with value {1}", campo.Key, value);
                    var entry = _context.Update(campo);
                    campo.Value = value;
                    entry.CurrentValues.SetValues(campo);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var fileStream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", documento.Path), FileMode.Open);

            WordDocument word = new(fileStream, FormatType.Automatic);
            fileStream.Close();

            word.Watermark = new TextWatermark("POC Forms", "Courier New", 250f, 100f)
            {
                Color = Color.Gray,
                Layout = WatermarkLayout.Diagonal
            };

            word.MailMerge.Execute([.. campos.Keys], [.. campos.Values]);

            var finalStream = new MemoryStream();
            word.Save(finalStream, FormatType.Docx);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "results", documento.Path);
            if (Directory.Exists(Path.GetDirectoryName(path)) == false)
            {
                _ = Directory.CreateDirectory(path: Path.GetDirectoryName(path));
            }
            using var newStream = new FileStream(path, FileMode.Create);
            await finalStream.CopyToAsync(newStream, cancellationToken);

            newStream.Flush();
            newStream.Close();
            finalStream.Position = 0;

            return File(finalStream, "application/octet-stream", "result.docx");
        }
    }
}