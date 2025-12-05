using System;

namespace BE_QLTiemThuoc.Dto
{
    public class ChiTietHoaDonCreateDto
    {
        // MaThuoc or TenThuoc should be provided (prefer MaThuoc if present)
        public string? MaThuoc { get; set; }
        public string? DonVi { get; set; }
        // Quantity to sell (packages when TrangThaiSeal = 0, units when = 1)
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string? MaLD { get; set; }
        public DateTime? HanSuDung { get; set; }
    }
    public class ChiTietHoaDonCreateOLDto
    {
        public string? MaThuoc { get; set; }
        public string? DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
}
