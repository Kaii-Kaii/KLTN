using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Services;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KhachHangController : ControllerBase
    {
        private readonly KhachHangService _service;
        private readonly AppDbContext _context;

        public KhachHangController(KhachHangService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KhachHang>>> GetAll()
        {
            var data = await _context.KhachHangs
                .GroupJoin(
                    _context.TaiKhoans,
                    kh => kh.MAKH,
                    tk => tk.MaKH,
                    (kh, tks) => new {
                        kh.MAKH,
                        kh.HoTen,
                        kh.NgaySinh,
                        kh.GioiTinh,
                        kh.DiaChi,
                        kh.DienThoai,
                        Email = tks.FirstOrDefault() != null ? tks.FirstOrDefault().EMAIL ?? "" : ""
                    }
                )
                .ToListAsync();
            
            return Ok(new {
                status = 1,
                message = "Lấy danh sách khách hàng thành công",
                data = data
            });
        }

        // GET: api/KhachHang/{maKhachHang}
        [HttpGet("{maKhachHang}")]
        public async Task<ActionResult<KhachHang>> GetById(string maKhachHang)
        {
            if (string.IsNullOrWhiteSpace(maKhachHang)) return BadRequest("maKhachHang is required");

            var kh = await _service.GetByIdAsync(maKhachHang);
            if (kh == null) return NotFound("Không tìm thấy khách hàng.");

            return Ok(kh);
        }


        [HttpPost]
        public async Task<ActionResult<KhachHang>> CreateKhachHang(KhachHang dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = created.MAKH }, created);
        }

        // PUT: api/KhachHang/{maKhachHang}
        [HttpPut("{maKhachHang}")]
        public async Task<IActionResult> UpdateKhachHang(string maKhachHang, KhachHang dto)
        {
            if (string.IsNullOrWhiteSpace(maKhachHang)) return BadRequest("maKhachHang is required");

            var updated = await _service.UpdateAsync(maKhachHang, dto);
            if (updated == null) return NotFound("Không tìm thấy khách hàng để cập nhật.");

            return Ok(updated);
        }
    }

}
