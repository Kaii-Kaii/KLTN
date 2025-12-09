using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Services;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaController : ControllerBase
    {
        private readonly MaService _service;

        public MaController(MaService service)
        {
            _service = service;
        }

        // GET: api/Ma/ByCode?code=ABC123
        [HttpGet("ByCode")]
        public async Task<IActionResult> GetByCode([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { status = 0, message = "Code is required" });
            }

            try
            {
                var result = await _service.GetByCodeAsync(code);
                if (result == null)
                {
                    return Ok(new { status = 0, message = "Không tìm thấy mã", data = (object?)null });
                }

                return Ok(new { 
                    status = 1, 
                    message = "Tìm thấy mã thành công", 
                    data = result 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
        }

        // GET: api/Ma/Search?code=AB
        [HttpGet("Search")]
        public async Task<IActionResult> SearchByCode([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { status = 0, message = "Code is required" });
            }

            try
            {
                var results = await _service.SearchByCodeAsync(code);
                return Ok(new { 
                    status = 1, 
                    message = $"Tìm thấy {results.Count} kết quả", 
                    data = results 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
        }

        // GET: api/Ma/{maCode}
        [HttpGet("{maCode}")]
        public async Task<IActionResult> GetByMa(string maCode)
        {
            if (string.IsNullOrWhiteSpace(maCode))
            {
                return BadRequest(new { status = 0, message = "MaCode is required" });
            }

            try
            {
                var result = await _service.GetByMaAsync(maCode);
                if (result == null)
                {
                    return Ok(new { status = 0, message = "Không tìm thấy mã", data = (object?)null });
                }

                return Ok(new { 
                    status = 1, 
                    message = "Tìm thấy mã thành công", 
                    data = result 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
        }

        // GET: api/Ma
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var results = await _service.GetAllAsync();
                return Ok(new { 
                    status = 1, 
                    message = "Lấy danh sách mã thành công", 
                    data = results 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = 0, message = ex.Message });
            }
        }
    }
}
