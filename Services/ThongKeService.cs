using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Dto;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Services
{
    public interface IThongKeService
    {
        Task<ThongKeResponse> GetThongKeNam(int year);
        Task<ThongKeResponse> GetThongKeThang(int month, int year);
        Task<List<TopSellingMedicineDto>> GetTopSellingMedicinesAsync(int year, int topCount = 10);
        Task<List<TopSellingMedicineDto>> GetTopSellingMedicinesByMonthAsync(int month, int year, int topCount = 10);
    }

    public class ThongKeService : IThongKeService
    {
        private readonly AppDbContext _context;

        public ThongKeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ThongKeResponse> GetThongKeNam(int year)
        {
            var response = new ThongKeResponse();

            // Initialize 12 months
            for (int i = 1; i <= 12; i++)
            {
                response.DoanhThu.Add(new ThongKeDto { Label = $"Tháng {i}", Value = 0 });
                response.TienNhapHang.Add(new ThongKeDto { Label = $"Tháng {i}", Value = 0 });
                response.TienHuyHang.Add(new ThongKeDto { Label = $"Tháng {i}", Value = 0 });
            }

            // 1. Doanh thu (HoaDon) - Only count paid invoices (TrangThai == true)
            var doanhThu = await _context.HoaDons
                .Where(h => h.NgayLap.Year == year && h.TrangThai == true)
                .GroupBy(h => h.NgayLap.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(h => h.TongTien) })
                .ToListAsync();

            foreach (var item in doanhThu)
            {
                response.DoanhThu[item.Month - 1].Value = item.Total;
            }

            // 2. Tien nhap hang (PhieuNhap)
            var tienNhap = await _context.PhieuNhaps
                .Where(p => p.NgayNhap.Year == year)
                .GroupBy(p => p.NgayNhap.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(p => p.TongTien) ?? 0 })
                .ToListAsync();

            foreach (var item in tienNhap)
            {
                response.TienNhapHang[item.Month - 1].Value = item.Total;
            }

            // 3. Tien huy hang (PhieuHuy)
            var tienHuy = await _context.PhieuHuys
                .Where(p => p.NgayHuy.Year == year)
                .GroupBy(p => p.NgayHuy.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(p => p.TongTien) ?? 0 })
                .ToListAsync();

            foreach (var item in tienHuy)
            {
                response.TienHuyHang[item.Month - 1].Value = item.Total;
            }

            return response;
        }

        public async Task<ThongKeResponse> GetThongKeThang(int month, int year)
        {
            var response = new ThongKeResponse();
            int daysInMonth = DateTime.DaysInMonth(year, month);

            // Initialize days
            for (int i = 1; i <= daysInMonth; i++)
            {
                response.DoanhThu.Add(new ThongKeDto { Label = $"{i}/{month}", Value = 0 });
                response.TienNhapHang.Add(new ThongKeDto { Label = $"{i}/{month}", Value = 0 });
                response.TienHuyHang.Add(new ThongKeDto { Label = $"{i}/{month}", Value = 0 });
            }

            // 1. Doanh thu
            var doanhThu = await _context.HoaDons
                .Where(h => h.NgayLap.Year == year && h.NgayLap.Month == month && h.TrangThai == true)
                .GroupBy(h => h.NgayLap.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(h => h.TongTien) })
                .ToListAsync();

            foreach (var item in doanhThu)
            {
                response.DoanhThu[item.Day - 1].Value = item.Total;
            }

            // 2. Tien nhap
            var tienNhap = await _context.PhieuNhaps
                .Where(p => p.NgayNhap.Year == year && p.NgayNhap.Month == month)
                .GroupBy(p => p.NgayNhap.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(p => p.TongTien) ?? 0 })
                .ToListAsync();

            foreach (var item in tienNhap)
            {
                response.TienNhapHang[item.Day - 1].Value = item.Total;
            }

            // 3. Tien huy
            var tienHuy = await _context.PhieuHuys
                .Where(p => p.NgayHuy.Year == year && p.NgayHuy.Month == month)
                .GroupBy(p => p.NgayHuy.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(p => p.TongTien) ?? 0 })
                .ToListAsync();

            foreach (var item in tienHuy)
            {
                response.TienHuyHang[item.Day - 1].Value = item.Total;
            }

            return response;
        }

        public async Task<List<TopSellingMedicineDto>> GetTopSellingMedicinesAsync(int year, int topCount = 10)
        {
            // Query using raw SQL or simplified LINQ that doesn't try to load Thuoc entity
            var result = await _context.ChiTietHoaDons
                .Join(_context.HoaDons,
                    ct => ct.MaHD,
                    h => h.MaHD,
                    (ct, h) => new { ChiTiet = ct, HoaDon = h })
                .Where(x => x.HoaDon.NgayLap.Year == year && x.HoaDon.TrangThai == true)
                .GroupBy(x => x.ChiTiet.MaThuoc)
                .Select(g => new TopSellingMedicineDto
                {
                    MaThuoc = g.Key ?? "N/A",
                    TenThuoc = g.Key ?? "Unknown",
                    TotalQuantity = g.Sum(x => x.ChiTiet.SoLuong),
                    TotalRevenue = g.Sum(x => x.ChiTiet.ThanhTien),
                    AveragePrice = g.Average(x => x.ChiTiet.DonGia),
                    CategoryName = "N/A"
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topCount + 10) // Get extra to combine into "Other"
                .ToListAsync();

            // Now enhance with medicine names from Thuoc table
            if (result.Any())
            {
                var maThuocList = result.Select(r => r.MaThuoc).ToList();
                var thuocNames = await _context.Thuoc
                    .Where(t => maThuocList.Contains(t.MaThuoc))
                    .Select(t => new { t.MaThuoc, t.TenThuoc, t.MaLoaiThuoc })
                    .ToListAsync();

                var loaiThuocs = await _context.LoaiThuoc.ToListAsync();

                foreach (var item in result)
                {
                    var thuoc = thuocNames.FirstOrDefault(t => t.MaThuoc == item.MaThuoc);
                    if (thuoc != null)
                    {
                        item.TenThuoc = thuoc.TenThuoc;
                        var loai = loaiThuocs.FirstOrDefault(l => l.MaLoaiThuoc == thuoc.MaLoaiThuoc);
                        item.CategoryName = loai?.TenLoaiThuoc ?? "N/A";
                    }
                }
            }

            return result;
        }

        public async Task<List<TopSellingMedicineDto>> GetTopSellingMedicinesByMonthAsync(int month, int year, int topCount = 10)
        {
            // Query using raw SQL or simplified LINQ that doesn't try to load Thuoc entity
            var result = await _context.ChiTietHoaDons
                .Join(_context.HoaDons,
                    ct => ct.MaHD,
                    h => h.MaHD,
                    (ct, h) => new { ChiTiet = ct, HoaDon = h })
                .Where(x => x.HoaDon.NgayLap.Year == year && x.HoaDon.NgayLap.Month == month && x.HoaDon.TrangThai == true)
                .GroupBy(x => x.ChiTiet.MaThuoc)
                .Select(g => new TopSellingMedicineDto
                {
                    MaThuoc = g.Key ?? "N/A",
                    TenThuoc = g.Key ?? "Unknown",
                    TotalQuantity = g.Sum(x => x.ChiTiet.SoLuong),
                    TotalRevenue = g.Sum(x => x.ChiTiet.ThanhTien),
                    AveragePrice = g.Average(x => x.ChiTiet.DonGia),
                    CategoryName = "N/A"
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(topCount + 10) // Get extra to combine into "Other"
                .ToListAsync();

            // Now enhance with medicine names from Thuoc table
            if (result.Any())
            {
                var maThuocList = result.Select(r => r.MaThuoc).ToList();
                var thuocNames = await _context.Thuoc
                    .Where(t => maThuocList.Contains(t.MaThuoc))
                    .Select(t => new { t.MaThuoc, t.TenThuoc, t.MaLoaiThuoc })
                    .ToListAsync();

                var loaiThuocs = await _context.LoaiThuoc.ToListAsync();

                foreach (var item in result)
                {
                    var thuoc = thuocNames.FirstOrDefault(t => t.MaThuoc == item.MaThuoc);
                    if (thuoc != null)
                    {
                        item.TenThuoc = thuoc.TenThuoc;
                        var loai = loaiThuocs.FirstOrDefault(l => l.MaLoaiThuoc == thuoc.MaLoaiThuoc);
                        item.CategoryName = loai?.TenLoaiThuoc ?? "N/A";
                    }
                }
            }

            return result;
        }
    }
}