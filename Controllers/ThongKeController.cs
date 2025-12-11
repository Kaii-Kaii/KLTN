using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BE_QLTiemThuoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOrStaff")]  
    public class ThongKeController : ControllerBase
    {
        private readonly IThongKeService _thongKeService;

        public ThongKeController(IThongKeService thongKeService)
        {
            _thongKeService = thongKeService;
        }

        [HttpGet("nam/{year}")]
        public async Task<ActionResult<ThongKeResponse>> GetThongKeNam(int year)
        {
            var result = await _thongKeService.GetThongKeNam(year);
            return Ok(result);
        }

        [HttpGet("thang/{month}/{year}")]
        public async Task<ActionResult<ThongKeResponse>> GetThongKeThang(int month, int year)
        {
            var result = await _thongKeService.GetThongKeThang(month, year);
            return Ok(result);
        }

        [HttpGet("top-selling/{year}")]
        public async Task<ActionResult<List<TopSellingMedicineDto>>> GetTopSellingMedicines(int year, [FromQuery] int topCount = 10)
        {
            var result = await _thongKeService.GetTopSellingMedicinesAsync(year, topCount);
            return Ok(result);
        }

        [HttpGet("top-selling/{month}/{year}")]
        public async Task<ActionResult<List<TopSellingMedicineDto>>> GetTopSellingMedicinesByMonth(int month, int year, [FromQuery] int topCount = 10)
        {
            var result = await _thongKeService.GetTopSellingMedicinesByMonthAsync(month, year, topCount);
            return Ok(result);
        }
    }
}