using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Thuoc
{
    public class LoaiThuoc
    {
        [Key]
        [StringLength(10)]
        public string MaLoaiThuoc { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string TenLoaiThuoc { get; set; } = null!;

        [StringLength(10)]
        public string? MaNhomLoai { get; set; }
    }
}
