using System.Collections.Generic;

namespace BE_QLTiemThuoc.Dto
{
    public class HoaDonCreateDto
    {
        public string? MaKH { get; set; }
        public string? MaNV { get; set; }
        public string? GhiChu { get; set; }
        public decimal? TongTien { get; set; }
        // 1: TIENMAT, 2: ONLINE, 3: OCD
        public int? PhuongThucTT { get; set; }
        // optional order code from payment gateway
        public string? OrderCode { get; set; }
        public List<ChiTietHoaDonCreateDto>? Items { get; set; }
    }
}
