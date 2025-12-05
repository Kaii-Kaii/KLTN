using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Services;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NhanVienController : ControllerBase
    {
        private readonly NhanVienService _service;

        public NhanVienController(NhanVienService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NhanVien>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NhanVien>> GetById(string id)
        {
            var nv = await _service.GetByIdAsync(id);
            if (nv == null)
                return NotFound();
            return Ok(nv);
        }

        [HttpPost]
        public async Task<ActionResult<NhanVien>> Create([FromBody] NhanVien nhanVien)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.CreateAsync(nhanVien);
                return CreatedAtAction(nameof(GetById), new { id = result.MaNV }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo nhân viên", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] NhanVien nhanVien)
        {
            try
            {
                if (id != nhanVien.MaNV)
                    return BadRequest("Mã nhân viên không khớp");

                await _service.UpdateAsync(nhanVien);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật nhân viên", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa nhân viên", error = ex.Message });
            }
        }
    }
}