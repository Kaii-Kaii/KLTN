using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhieuXuLyHoanHuyController : ControllerBase
    {
        private readonly PhieuXuLyHoanHuyService _service;

        public PhieuXuLyHoanHuyController(PhieuXuLyHoanHuyService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] string? maNV_Tao, [FromQuery] bool? loaiNguon, [FromQuery] int? trangThai)
        {
            try
            {
                var list = await _service.GetAllAsync(start, end, maNV_Tao, loaiNguon, trangThai);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Details/{maPXH}")]
        public async Task<IActionResult> Details(string maPXH)
        {
            if (string.IsNullOrWhiteSpace(maPXH)) return BadRequest("MaPXH is required");
            try
            {
                var details = await _service.GetDetailsAsync(maPXH);
                if (details == null) return NotFound();
                return Ok(details);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/PhieuXuLyHoanHuy/Requests (pending TrangThai default)
        [HttpGet("Requests")]
        public async Task<IActionResult> Requests([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            try
            {
                var list = await _service.GetRequestsAsync(start, end, null);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/PhieuXuLyHoanHuy/ApprovedWithCancelStatus?start=...&end=...
        [HttpGet("ApprovedWithCancelStatus")]
        public async Task<IActionResult> ApprovedWithCancelStatus([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] string? maNV_Tao, [FromQuery] bool? loaiNguon)
        {
            try
            {
                var list = await _service.GetApprovedWithCancelStatusAsync(start, end, maNV_Tao, loaiNguon);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPost("QuickCreate")]
        public async Task<IActionResult> QuickCreate([FromBody]PhieuXuLyHoanHuyQuickCreateDto dto)
        {
            if (dto == null) return BadRequest(new ApiResponse<string> { Status = -1, Message = "Payload is required", Data = null });

            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var result = await _service.CreateQuickRequestAsync(dto);
                return result;
            });

            return Ok(response);
        }
        
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] PhieuXuLyHoanHuyCreateDto dto)
        {
            if (dto == null) return BadRequest(new ApiResponse<string> { Status = -1, Message = "Payload is required", Data = null });

            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var result = await _service.CreateFromDetailsAsync(dto);
                return result;
            });

            return Ok(response);
        }

        // POST: api/PhieuXuLyHoanHuy/Approve/{maPXH}
        // Body: { MaNV_Duyet: "NVxxx" }
        public class ApproveDto { public string? MaNV_Duyet { get; set; } }

        [HttpPost("Approve/{maPXH}")]
        public async Task<IActionResult> Approve(string maPXH, [FromBody] ApproveDto dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(maPXH)) throw new ArgumentException("maPXH is required");
                if (dto == null || string.IsNullOrWhiteSpace(dto.MaNV_Duyet)) throw new ArgumentException("MaNV_Duyet is required");
                await _service.ApproveAsync(maPXH, dto.MaNV_Duyet!);
                return new { MaPXH = maPXH, ApprovedBy = dto.MaNV_Duyet };
            });

            return Ok(response);
        }

        // PUT: api/PhieuXuLyHoanHuy/Update
        // Body: UpdatePhieuXuLyHoanHuyDto { MaPXH, TrangThai?, GhiChu?, MaNV_Duyet?, ChiTiets?: [{ MaCTPXH, SoLuong?, LoaiXuLy? }] }
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] UpdatePhieuXuLyHoanHuyDto dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                if (dto == null) throw new ArgumentNullException(nameof(dto));
                if (string.IsNullOrWhiteSpace(dto.MaPXH)) throw new ArgumentException("MaPXH is required");
                await _service.UpdateWithDetailsAsync(dto);
                return new { MaPXH = dto.MaPXH, Updated = true };
            });

            return Ok(response);
        }
        
    }
}