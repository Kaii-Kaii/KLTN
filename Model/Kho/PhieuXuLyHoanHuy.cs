using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BE_QLTiemThuoc.Model;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("PhieuXuLyHoanHuy")]
    public class PhieuXuLyHoanHuy
    {
        [Key]
        [StringLength(20)]
        public string MaPXH { get; set; } = null!;

        public DateTime? NgayTao { get; set; }

        [StringLength(10)]
        public string? MaNV_Tao { get; set; }

        [StringLength(10)]
        public string? MaNV_Duyet { get; set; }

        [StringLength(20)]
        public string? MaHD { get; set; }

        public bool? LoaiNguon { get; set; } // 0: từ kho, 1: từ hoá đơn

        public int? TrangThai { get; set; } // 0: Chờ duyệt, 1: Đã duyệt

        [StringLength(500)]
        public string? GhiChu { get; set; }

    }
}