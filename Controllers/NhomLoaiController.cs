using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Services;
using BE_QLTiemThuoc.Model.Thuoc;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhomLoaiController : ControllerBase
    {
        private readonly NhomLoaiService _service;

        public NhomLoaiController(NhomLoaiService service)
        {
            _service = service;
        }

        // GET: api/NhomLoai/Loai/{maNhom}
        [HttpGet("Loai/{maNhom}")]
        public async Task<IActionResult> GetLoaiByNhom(string maNhom)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var list = await _service.GetLoaiByNhomAsync(maNhom);
                return list;
            });
            return Ok(response);
        }

        // GET: api/NhomLoai/WithLoai
        [HttpGet("WithLoai")]
        public async Task<IActionResult> GetGroupsWithLoai()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var groups = await _service.GetGroupsWithLoaiAsync();
                return groups;
            });

            return Ok(response);
        }

        // POST: api/NhomLoai
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NhomLoai dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var (ok, error) = await _service.CreateAsync(dto);
                if (!ok) throw new Exception(error);
                return dto;
            });

            return Ok(response);
        }

        // PUT: api/NhomLoai/{maNhom}
        [HttpPut("{maNhom}")]
        public async Task<IActionResult> Update(string maNhom, [FromBody] NhomLoai dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var (ok, error) = await _service.UpdateAsync(maNhom, dto);
                if (!ok) throw new Exception(error);
                return dto;
            });

            return Ok(response);
        }

        // DELETE: api/NhomLoai/{maNhom}
        [HttpDelete("{maNhom}")]
        public async Task<IActionResult> Delete(string maNhom)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var (ok, error) = await _service.DeleteAsync(maNhom);
                if (!ok) throw new Exception(error);
                return true;
            });

            return Ok(response);
        }
    }
}
