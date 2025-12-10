using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThuocController : ControllerBase
    {
        private readonly ThuocService _service;

        public ThuocController(ThuocService service)
        {
   _service = service;
        }

        // Helper: extract filename from a provided URL or path. Returns null if input empty.
        private static string? ExtractFileNameFromUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            try
            {
                var s = input.Trim();
                // If absolute URL, use LocalPath
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(s);
                    s = uri.LocalPath ?? s;
                }
                // strip querystring
                var noQuery = s.Split('?')[0];
                noQuery = noQuery.Trim('/');
                var file = Path.GetFileName(noQuery);
                return string.IsNullOrEmpty(file) ? null : file;
            }
            catch
            {
                try
                {
                    var t = input.Split('?')[0].Trim('/');
                    var f = Path.GetFileName(t);
                    return string.IsNullOrEmpty(f) ? null : f;
                }
                catch
                {
                    return null;
                }
            }
        }

        // GET: api/Thuoc/TopLoaiThuoc
        [HttpGet("TopLoaiThuoc")]
    [AllowAnonymous]  // 🔓 Public - khách có thể xem
  public async Task<IActionResult> GetTopLoaiThuoc()
   {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(() => _service.GetTopLoaiThuocAsync());

            return Ok(response);
        }

        // GET: api/Thuoc
        [HttpGet]
  [AllowAnonymous]  // 🔓 Public - khách có thể xem danh sách thuốc
        public async Task<IActionResult> GetThuoc()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                return await _service.GetThuocAsync();
            });

            return Ok(response);
        }

        // GET: api/Thuoc/LoaiDonVi
        [HttpGet("LoaiDonVi")]
        [AllowAnonymous]  // 🔓 Public
        public async Task<IActionResult> GetLoaiDonVi()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                return await _service.GetLoaiDonViAsync();
            });

            return Ok(response);
        }


        // GET: api/ListThuocTonKho
      [HttpGet("ListThuocTonKho")]
       [AllowAnonymous]  // 🔐 Chỉ Admin hoặc Staff xem tồn kho
        public async Task<IActionResult> GetListThuocTonKho()
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                return await _service.GetListThuocTonKhoAsync();
            });

            return Ok(response);
        }
        // GET: api/Thuoc/ByLoaiTonKho/{maLoaiThuoc}
     [HttpGet("ByLoaiTonKho/{maLoaiThuoc}")]
        [AllowAnonymous]  // 🔐 Chỉ Admin hoặc Staff xem tồn kho
        public async Task<IActionResult> GetThuocByLoaiTonKho(string maLoaiThuoc)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                return await _service.GetThuocByLoaiTonKhoAsync(maLoaiThuoc);
            });

            return Ok(response);
        }
        // GET: api/Thuoc/ByLoai/{maLoaiThuoc}
        [HttpGet("ByLoai/{maLoaiThuoc}")]
 [AllowAnonymous]  // 🔓 Public
 public async Task<IActionResult> GetThuocByLoai(string maLoaiThuoc)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                return await _service.GetThuocByLoaiAsync(maLoaiThuoc);
            });

            return Ok(response);
        }

        // GET: api/Thuoc/{maThuoc}
        [HttpGet("{maThuoc}")]
        [AllowAnonymous]  // 🔓 Public - khách có thể xem chi tiết thuốc
public async Task<IActionResult> GetThuocById(string maThuoc)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var thuoc = await _service.GetThuocByIdAsync(maThuoc);
                if (thuoc == null) throw new Exception("Không tìm thấy thuốc.");
                return thuoc;
            });

            return Ok(response);
        }

      // GET: api/Thuoc/{maThuoc}/GiaThuocs
        [HttpGet("{maThuoc}/GiaThuocs")]
        [AllowAnonymous]  // 🔓 Public
   public async Task<IActionResult> GetGiaThuocs(string maThuoc)
     {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () => await _service.GetGiaThuocsByMaThuocAsync(maThuoc));
            return Ok(response);
        }

   // POST: api/Thuoc
        [HttpPost]
   [Authorize(Policy = "AdminOrStaff")]  // 🔐 Chỉ Admin hoặc Staff tạo thuốc mới
        public async Task<IActionResult> PostThuoc([FromForm] ThuocDto thuocDto)
     {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () => await _service.CreateThuocAsync(thuocDto, Request));
            return Ok(response);
        }

        // PUT: api/Thuoc/{id}
        [HttpPut("{id}")]
     [Authorize(Policy = "AdminOrStaff")]  // 🔐 Chỉ Admin hoặc Staff cập nhật
        public async Task<IActionResult> PutThuoc(string id, [FromForm] ThuocDto thuocDto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () => await _service.UpdateThuocAsync(id, thuocDto, Request));

            return Ok(response);
        }

        // DELETE: api/Thuoc/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]  // 🔐 Chỉ Admin mới được xoá thuốc
     public async Task<IActionResult> DeleteThuoc(string id)
     {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () => await _service.DeleteThuocAsync(id));
            return Ok(response);
        }

        // GET: api/Thuoc/ByCode/{code}
        // Search thuốc by Code - returns list of MaThuoc, TenThuoc, Code
        [HttpGet("ByCode/{code}")]
        [Authorize(Policy = "AdminOrStaff")] 
        public async Task<IActionResult> GetThuocByCode(string code)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var thuocs = await _service.GetThuocByCodeAsync(code);
                return thuocs;
            });

            return Ok(response);
        }

        // GET: api/Thuoc/SearchCode/{code}
        // Find exact match by Code
        [HttpGet("SearchCode/{code}")]
        [Authorize(Policy = "AdminOrStaff")] 
        public async Task<IActionResult> GetThuocByCodeExact(string code)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var thuoc = await _service.GetThuocByCodeExactAsync(code);
                if (thuoc == null) throw new Exception("Không tìm thấy thuốc với mã này.");
                return thuoc;
            });

            return Ok(response);
        }

        
    }
}
