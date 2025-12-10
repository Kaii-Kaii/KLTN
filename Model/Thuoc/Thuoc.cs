using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_QLTiemThuoc.Model.Thuoc
{
    [Table("Thuoc")]
    public class Thuoc
    {
        [Key]
        public string MaThuoc { get; set; } = null!;

        public string? MaLoaiThuoc { get; set; }

        [Required]
        public string TenThuoc { get; set; } = null!;

        public string? ThanhPhan { get; set; }
        public string? MoTa { get; set; }
        public string? CongDung { get; set; }
        public string? CachDung { get; set; }
        public string? LuuY { get; set; }
        public string? UrlAnh { get; set; }
        public string? MaNCC { get; set; }
        public string? Code { get; set; }
    }
}
