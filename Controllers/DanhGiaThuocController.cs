using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BE_QLTiemThuoc.Controllers
{
 [ApiController]
 [Route("api/[controller]")]
 public class DanhGiaThuocController : ControllerBase
 {
 private readonly DanhGiaThuocService _service;
 public DanhGiaThuocController(DanhGiaThuocService service)
 {
 _service = service;
 }

 [HttpGet("{maDanhGia}")]
 public async Task<IActionResult> Get(string maDanhGia) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetAsync(maDanhGia)));

 [HttpGet("thuoc/{maThuoc}")]
 public async Task<IActionResult> GetByThuoc(string maThuoc) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetByThuocAsync(maThuoc)));

 [HttpGet("khachhang/{maKh}")]
 public async Task<IActionResult> GetByKhachHang(string maKh) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetByKhachHangAsync(maKh)));

 [HttpGet("eligible/{maKh}")]
 public async Task<IActionResult> GetEligible(string maKh) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.GetEligibleThuocAsync(maKh)));

 [HttpPost]
 public async Task<IActionResult> CreateOrUpdate([FromBody] DanhGiaThuocCreateDto dto) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.CreateOrUpdateAsync(dto)));

 [HttpPut("{maDanhGia}")]
 public async Task<IActionResult> Update(string maDanhGia, [FromBody] DanhGiaThuocUpdateDto dto) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(()=> _service.UpdateAsync(maDanhGia, dto)));

 [HttpDelete("{maDanhGia}")]
 public async Task<IActionResult> Delete(string maDanhGia) => Ok(await ApiResponseHelper.ExecuteSafetyAsync(async ()=> await _service.DeleteAsync(maDanhGia)));
 }
}
