using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Services;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOrStaff")]// 🔐 Chỉ Admin hoặc Staff quản lý nhân viên
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

        [HttpGet("usernames")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableUsernames()
        {
            try
            {
                var usernames = await _service.GetAllUsernamesAsync();
                return Ok(usernames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách tên đăng nhập", error = ex.Message });
            }
        }

        [HttpGet("{id}/account-status")]
        public async Task<ActionResult> GetAccountStatus(string id)
        {
            try
            {
                var nhanVien = await _service.GetByIdAsync(id);
                if (nhanVien == null)
                    return NotFound(new { success = false, message = "Không tìm thấy nhân viên" });

                var accountStatus = await _service.GetAccountStatusAsync(id);
                return Ok(new { success = true, data = accountStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy trạng thái tài khoản", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NhanVien>> GetById(string id)
        {
            var nv = await _service.GetByIdAsync(id);
            if (nv == null)
                return NotFound();
            return Ok(nv);
        }

        [HttpGet("{id}/with-account")]
        public async Task<ActionResult> GetByIdWithAccount(string id)
        {
            try
            {
                var nv = await _service.GetByIdAsync(id);
                if (nv == null)
                    return NotFound(new { message = "Không tìm thấy nhân viên" });

                // Lấy thông tin tài khoản
                var taiKhoan = await _service.GetAccountInfoAsync(id);
                
                return Ok(new {
                    maNV = nv.MaNV,
                    hoTen = nv.HoTen,
                    dienThoai = nv.DienThoai,
                    diaChi = nv.DiaChi,
                    email = taiKhoan?.EMAIL,
                    ngaySinh = nv.NgaySinh,
                    gioiTinh = nv.GioiTinh,
                    chucVu = nv.ChucVu,
                    tenDangNhap = taiKhoan?.TenDangNhap
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin nhân viên", error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint mới: Tạo nhân viên cùng với tài khoản đăng nhập
        /// </summary>
        [HttpPost("create-with-account")]
        [Authorize(Policy = "AdminOnly")]  // 🔐 Chỉ Admin mới được tạo nhân viên
        public async Task<ActionResult<NhanVienCreateResponseDto>> CreateWithAccount([FromBody] NhanVienCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });

                var result = await _service.CreateWithAccountAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = result.MaNV }, new
                {
                    success = true,
                    message = result.Message,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo nhân viên",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]  // 🔐 Chỉ Admin mới được cập nhật nhân viên
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
        [Authorize(Policy = "AdminOnly")]  // 🔐 Chỉ Admin mới được xoá nhân viên
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

        [HttpPost("{id}/disable-account")]
        [Authorize(Policy = "AdminOnly")]  // 🔐 Chỉ Admin mới được vô hiệu hoá
        public async Task<IActionResult> DisableAccount(string id)
        {
            try
            {
                var result = await _service.DisableAccountAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Không tìm thấy nhân viên" });

                return Ok(new { success = true, message = "Tài khoản đã được vô hiệu hoá" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi vô hiệu hoá tài khoản", error = ex.Message });
            }
        }

        [HttpPost("{id}/enable-account")]
        [Authorize(Policy = "AdminOnly")]  // 🔐 Chỉ Admin mới được mở lại
        public async Task<IActionResult> EnableAccount(string id)
        {
            try
            {
                var result = await _service.EnableAccountAsync(id);
                if (!result)
                    return NotFound(new { success = false, message = "Không tìm thấy nhân viên" });

                return Ok(new { success = true, message = "Tài khoản đã được mở lại" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi mở tài khoản", error = ex.Message });
            }
        }
    }
}