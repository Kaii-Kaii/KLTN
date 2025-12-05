using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BE_QLTiemThuoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoaiDonViController : ControllerBase
    {
        private readonly LoaiDonViService _service;
        public LoaiDonViController(LoaiDonViService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LoaiDonVi dto)
        {
            var response = await ApiResponseHelper.ExecuteSafetyAsync(async () =>
            {
                var res = await _service.CreateAsync(dto);
                if (!res.Ok) throw new Exception(res.Error ?? "Create failed");
                // return the created item as Data
                return dto;
            });

            if (response.Status != 1)
                return BadRequest(response);

            return Created(string.Empty, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] LoaiDonVi dto)
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
    }
}
