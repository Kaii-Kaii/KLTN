using Microsoft.AspNetCore.Mvc;
using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Services;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhaCungCapController : ControllerBase
    {
        private readonly NhaCungCapService _service;

        public NhaCungCapController(NhaCungCapService service)
        {
            _service = service;
        }

        // GET: api/NhaCungCap
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

        // GET: api/NhaCungCap/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var item = await _service.GetByIdAsync(id);
                return item;
            });
            return Ok(response);
        }

        // POST: api/NhaCungCap
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NhaCungCap ncc)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                if (!ModelState.IsValid) throw new Exception("Dữ liệu không hợp lệ.");
                var created = await _service.CreateAsync(ncc);
                return created;
            });
            return Ok(response);
        }

        // PUT: api/NhaCungCap/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] NhaCungCap ncc)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                if (!ModelState.IsValid) throw new Exception("Dữ liệu không hợp lệ.");
                var updated = await _service.UpdateAsync(id, ncc);
                return updated;
            });
            return Ok(response);
        }

        // DELETE: api/NhaCungCap/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var result = await _service.DeleteAsync(id);
                return result;
            });
            return Ok(response);
        }
    }
}
