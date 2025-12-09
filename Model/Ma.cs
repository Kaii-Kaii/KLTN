using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Model
{
    public class Ma
    {
        [Key]
        public string? MaCode { get; set; }

        public string? Code { get; set; }
    }
}
