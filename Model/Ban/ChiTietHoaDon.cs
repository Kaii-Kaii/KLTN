using System;
using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Model
{
    public class ChiTietHoaDon
    {
        // Primary key (newly added in DB schema)
    public string MaCTHD { get; set; } = string.Empty;
    public string MaHD { get; set; } = string.Empty;
    public string? MaLo { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string? MaLD { get; set; }
    [StringLength(10)]
    public string? MaLoaiDonVi { get; set; }
    [StringLength(10)]
    public string? MaThuoc { get; set; }
    public DateTime? HanSuDung { get; set; }
    // Indicates whether the line has been processed/allocated (BIT in DB). Default 0 in DB.
    public bool TrangThaiXuLy { get; set; } = false;
    }
}
