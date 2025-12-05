using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoaiThuocController : ControllerBase
    {
        private readonly LoaiThuocService _service;
        private readonly NhomLoaiService _nhomService;
        private readonly ThuocService _thuocService;

        public LoaiThuocController(LoaiThuocService service, NhomLoaiService nhomService, ThuocService thuocService)
        {
            _service = service;
            _nhomService = nhomService;
            _thuocService = thuocService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var list = await _service.GetAllAsync();
                // Project to a response shape that does not include any Icon field
                var projected = list.Select(x => new {
                    MaLoaiThuoc = x.MaLoaiThuoc,
                    TenLoaiThuoc = x.TenLoaiThuoc,
                    MaNhomLoai = x.MaNhomLoai
                }).ToList();
                return projected;
            });

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoaiThuoc dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var res = await _service.CreateAsync(dto);
                if (!res.Ok) throw new Exception(res.Error ?? "Create failed");
                return dto;
            });

            if (response.Status != 1) return BadRequest(response);
            return Created(string.Empty, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] LoaiThuoc dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var res = await _service.UpdateAsync(id, dto);
                return res;
            });

            if (response.Status != 1) return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var res = await _service.DeleteAsync(id);
                return res;
            });

            if (response.Status != 1) return BadRequest(response);
            return Ok(response);
        }

        // GET: api/LoaiThuoc/{id}/Details
        [HttpGet("{id}/Details")]
        public async Task<IActionResult> Details(string id)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var loai = await _service.GetByIdAsync(id);
                if (loai == null) throw new Exception("LoaiThuoc not found");
                var thuocs = await _thuocService.GetThuocNamesByLoaiAsync(id);
                var nhom = string.IsNullOrWhiteSpace(loai.MaNhomLoai) ? null : await _nhomService.GetByIdAsync(loai.MaNhomLoai!);
                return new {
                    Loai = loai,
                    TenNhomLoai = nhom?.TenNhomLoai,
                    Thuocs = thuocs
                };
            });

            if (response.Status != 1) return BadRequest(response);
            return Ok(response);
        }
    }
}
