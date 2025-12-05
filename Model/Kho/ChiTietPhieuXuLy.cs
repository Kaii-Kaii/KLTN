using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Kho
{
    [Table("ChiTietPhieuXuLy")]
    public class ChiTietPhieuXuLy
    {
        [Key]
        [StringLength(20)]
        public string MaCTPXH { get; set; } = null!;

        [StringLength(20)]
        public string? MaPXH { get; set; }

        [StringLength(20)]
        public string? MaLo { get; set; }

        public int? SoLuong { get; set; }

        public bool? LoaiXuLy { get; set; } // 0: Huỷ luôn, 1: Nhập kho lại
    }
}
