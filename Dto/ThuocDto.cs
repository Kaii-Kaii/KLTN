using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Dto
{
    public class ThuocDto
    {
        public string MaLoaiThuoc { get; set; }
        public string? MaThuoc { get; set; }
        public string? TenThuoc { get; set; }
        public string? ThanhPhan { get; set; }
        public string? MoTa { get; set; }
        public string? CongDung { get; set; }
        public string? CachDung { get; set; }
        public string? LuuY { get; set; }
        public string? UrlAnh { get; set; }
        public string MaNCC { get; set; }
        public string? Code { get; set; }
        public IFormFile? FileAnh { get; set; }
         public List<GiaThuocDto>? GiaThuocs { get; set; }
    }
}
