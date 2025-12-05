using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("ChiTietPhieuNhap")]
    public class ChiTietPhieuNhap
    {
        [Key]
        [StringLength(20)]
        public string MaCTPN { get; set; } = null!;

        [StringLength(20)]
        public string MaPN { get; set; } = null!;

        [StringLength(10)]
        public string MaThuoc { get; set; } = null!;

        [StringLength(20)]
        public string? MaLo { get; set; }

        [StringLength(10)]
        public string? MaLoaiDonVi { get; set; }

        public int SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DonGia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ThanhTien { get; set; }

    // New columns per latest schema
    [Column(TypeName = "date")] // match SQL: ChiTietPhieuNhap.HanSuDung DATE (nullable)
    public DateTime? HanSuDung { get; set; }

        // NVARCHAR(MAX) by default in EF when no StringLength specified
        public string? GhiChu { get; set; }
    }
}