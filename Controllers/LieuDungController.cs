using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Model;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LieuDungController : ControllerBase
    {
        private readonly LieuDungService _service;

        public LieuDungController(LieuDungService service)
        {
            _service = service;
        }

        // GET: api/LieuDung
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var list = await _service.GetAllAsync();
                return list;
            });

            return Ok(response);
        }

        // POST: api/LieuDung
        [HttpPost]
        public async Task<IActionResult> Create(LieuDung dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var created = await _service.CreateAsync(dto);
                return created;
            });

            return CreatedAtAction(nameof(GetById), new { maLD = response.Data?.MaLD }, response);
        }

        // GET: api/LieuDung/{maLD}
        [HttpGet("{maLD}")]
        public async Task<IActionResult> GetById(string maLD)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var item = await _service.GetByIdAsync(maLD);
                return item;
            });

            if (response.Data == null)
            {
                return NotFound(new { status = 1, message = "Không tìm thấy liều dùng." });
            }

            return Ok(response);
        }

        // PUT: api/LieuDung/{maLD}
        [HttpPut("{maLD}")]
        public async Task<IActionResult> Update(string maLD, LieuDung dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var updated = await _service.UpdateAsync(maLD, dto);
                return updated;
            });

            if (response.Data == null)
            {
                return NotFound(new { status = 1, message = "Không tìm thấy liều dùng để cập nhật." });
            }

            return Ok(response);
        }

        // DELETE: api/LieuDung/{maLD}
        [HttpDelete("{maLD}")]
        public async Task<IActionResult> Delete(string maLD)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var deleted = await _service.DeleteAsync(maLD);
                return deleted;
            });

            if (!(bool)response.Data!)
            {
                return NotFound(new { status = 1, message = "Không tìm thấy liều dùng để xóa." });
            }

            return Ok(new { status = 0, message = "Xóa liều dùng thành công." });
        }
    }
}
