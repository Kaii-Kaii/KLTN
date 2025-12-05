using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Model.Thuoc
{
    public class NhomLoai
    {
        [Key]
        public string? MaNhomLoai { get; set; }

        public string? TenNhomLoai { get; set; }
    }
}
