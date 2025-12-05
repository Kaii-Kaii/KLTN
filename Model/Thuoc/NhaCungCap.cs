using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Thuoc
{
    [Table("NhaCungCap")]
    public class NhaCungCap
    {
        [Key]
        [StringLength(10)]
        public string MaNCC { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string TenNCC { get; set; } = null!;

        [StringLength(200)]
        public string? DiaChi { get; set; }

        [StringLength(20)]
        public string? DienThoai { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }
    }
}
