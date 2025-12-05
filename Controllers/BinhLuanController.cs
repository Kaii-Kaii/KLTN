using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BE_QLTiemThuoc.Controllers
{
 [ApiController]
 [Route("api/[controller]")]
 public class BinhLuanController : ControllerBase
 {
 private readonly BinhLuanService _service;
 public BinhLuanController(BinhLuanService service){ _service = service; }

 [HttpGet("{maBL}")]
 public async Task<IActionResult> Get(string maBL) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetAsync(maBL)));

 [HttpGet("thuoc/{maThuoc}")]
 public async Task<IActionResult> GetByThuoc(string maThuoc) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetByThuocAsync(maThuoc)));

 // Admin: only root comments for a product
 [HttpGet("thuoc/{maThuoc}/roots")]
 public async Task<IActionResult> GetRootsByThuoc(string maThuoc) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetRootByThuocAsync(maThuoc)));

 // Admin: unanswered comments grouped in root->child order
 [HttpGet("thuoc/{maThuoc}/unanswered")]
 public async Task<IActionResult> GetUnansweredByThuoc(string maThuoc) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetUnansweredByThuocAsync(maThuoc)));

 // Global admin view: list all roots with status (0/1)
 [HttpGet("roots/status")]
 public async Task<IActionResult> GetGlobalRootStatus() => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetGlobalRootStatusAsync()));

 [HttpPost]
 public async Task<IActionResult> Create([FromBody] BinhLuanCreateDto dto) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.CreateAsync(dto)));

 [HttpDelete("{maBL}")]
 public async Task<IActionResult> Delete(string maBL) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.DeleteAsync(maBL)));
 }
}
