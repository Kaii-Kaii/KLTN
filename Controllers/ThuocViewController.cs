using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThuocViewController : ControllerBase
    {
        private readonly ThuocViewService _service;

        public ThuocViewController(ThuocViewService service)
        {
            _service = service;
        }

        // GET: api/ThuocView/ChuaTachLe
        // Optional query: ?status=0|1
        // - status=0 -> show lots with SoLuongCon == 0 (empty)
        // - status=1 -> show lots with SoLuongCon > 0
        // - not provided -> show all lots regardless of SoLuongCon
        [HttpGet("ChuaTachLe")]
        public async Task<IActionResult> GetChuaTachLe([FromQuery] int? status)
        {
            if (status != null && status != 0 && status != 1)
                return BadRequest(new { error = "status must be 0 or 1 if provided" });

            var list = await _service.GetChuaTachLeAsync(status);
            return Ok(list);
        }

        // GET: api/ThuocView/DaTachLe
        // Optional query: ?status=0|1 (same semantics as ChuaTachLe)
        [HttpGet("DaTachLe")]
        public async Task<IActionResult> GetDaTachLe([FromQuery] int? status)
        {
            if (status != null && status != 0 && status != 1)
                return BadRequest(new { error = "status must be 0 or 1 if provided" });

            var list = await _service.GetDaTachLeAsync(status);
            return Ok(list);
        }

        // GET: api/ThuocView/TongSoLuongCon
        [HttpGet("TongSoLuongCon")]
        public async Task<IActionResult> GetTongSoLuongCon()
        {
            var list = await _service.GetTongSoLuongConAsync();
            return Ok(list);
        }

        // GET: api/ThuocView/SapHetHan
        // Options (query):
        // - ?days=7        -> next 7 days (from today)
        // - ?months=2      -> next 2 months (from today)
        // - ?years=1       -> next 1 year (from today)
        // - ?fromDate=2025-11-01 -> from that date up to today
        // If multiple params provided, precedence is: fromDate, days, months, years.
        [HttpGet("SapHetHan")]
        public async Task<IActionResult> GetSapHetHan([FromQuery] int? days, [FromQuery] int? months, [FromQuery] int? years, [FromQuery] DateTime? fromDate)
        {
            // validate fromDate if provided: must be <= today
            if (fromDate != null && fromDate.Value.Date > DateTime.Now.Date)
                return BadRequest(new { error = "fromDate must be today or earlier (tính từ ngày đó đến giờ)." });

            var list = await _service.GetSapHetHanAsync(days, months, years, fromDate);
            return Ok(list);
        }

        // GET: api/ThuocView/LichSuLo/{maLo}
        // Returns history of a lot via PhieuNhap and ChiTietPhieuNhap
        [HttpGet("LichSuLo/{maLo}")]
        public async Task<IActionResult> GetLichSuLo(string maLo)
        {
            if (string.IsNullOrWhiteSpace(maLo)) return BadRequest(new { error = "maLo is required" });

            var list = await _service.GetLichSuLoAsync(maLo);
            return Ok(list);
        }
    }
}
