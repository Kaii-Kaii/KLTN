using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOrStaff")]  // ?? Ch? Admin ho?c Staff qu?n lý phi?u hu?
    public class PhieuHuyController : ControllerBase
    {
        private readonly PhieuHuyService _service;

        public PhieuHuyController(PhieuHuyService service)
        {
            _service = service;
        }

        // GET: api/PhieuHuy?start=...&end=...&maNV=...
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] string? maNV)
        {
            try
            {
                var list = await _service.GetAllAsync(start, end, maNV);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/PhieuHuy/Details/{maPH}
        [HttpGet("Details/{maPH}")]
        public async Task<IActionResult> Details(string maPH)
        {
            if (string.IsNullOrWhiteSpace(maPH)) return BadRequest("MaPH is required");
            try
            {
                var item = await _service.GetDetailsAsync(maPH);
                if (item == null) return NotFound();
                return Ok(item);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // POST: api/PhieuHuy/CreateByPXH
        [HttpPost("CreateByPXH")]
        public async Task<IActionResult> CreateByPXH([FromBody] CreateByPxhDto dto)
        {
            if (dto == null) return BadRequest("Payload is required");
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var res = await _service.CreateFromPXHAsync(dto);
                return res;
            });
            return Ok(response);
        }
    }
}