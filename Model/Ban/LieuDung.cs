using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Model
{
    public class LieuDung
    {
        [Key]
        public string MaLD { get; set; } = string.Empty;
        public string? TenLieuDung { get; set; }
    }
}
