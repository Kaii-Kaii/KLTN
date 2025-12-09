using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_QLTiemThuoc.Services;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOrStaff")]  // ?? Ch? Admin ho?c Staff qu?n lý hình ?nh
    public class ImagesController : ControllerBase
    {
        private readonly ImagesService _service;

        public ImagesController(ImagesService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("List")]
        public async Task<IActionResult> List()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var files = await _service.ListAsync();
                return files;
            });

            return Ok(response);
        }

    public class ImportRequest { public string? url { get; set; } }

    // Wrapper for file uploads so Swagger can describe the multipart/form-data schema
    public class UploadFileRequest
    {
        public IFormFile? file { get; set; }
    }

        [HttpPost("UploadExternal")]
        public async Task<IActionResult> UploadExternal([FromBody] ImportRequest req)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var file = await _service.UploadExternalAsync(req?.url);
                return file;
            });

            return Ok(response);
        }

    [HttpPost("UploadFile")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest req)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var file = req?.file;
                var name = await _service.UploadFileAsync(file!);
                return name;
            });

            return Ok(response);
        }

        // Upload directly into FE product folder (handles duplicate names)
        [HttpPost("UploadToProduct")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadToProduct([FromForm] UploadFileRequest req)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var file = req?.file;
                var name = await _service.UploadToProductAsync(file!);
                return name;
            });

            return Ok(response);
        }

        // Serve a temp-uploaded file for preview before finalizing
        [HttpGet("GetTemp")]
        public IActionResult GetTemp([FromQuery] string? filename)
        {
            try
            {
                var tup = _service.GetTempFile(filename);
                return File(tup.bytes, tup.contentType);
            }
            catch
            {
                return NotFound();
            }
        }

        // Move file from temp to product folder (called when user clicks Save)
        [HttpPost("FinalizeImage")]
        public async Task<IActionResult> FinalizeImage([FromBody] dynamic req)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var fileName = req?.fileName as string;
                var name = await _service.FinalizeImageAsync(fileName!);
                return name;
            });

            return Ok(response);
        }

        // wrapper types preserved for API surface
        // basic filename sanitizer is now in service
        
    }
}
