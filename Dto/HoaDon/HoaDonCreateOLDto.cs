using System.Collections.Generic;

namespace BE_QLTiemThuoc.Dto
{
    // DTO for creating online invoices (HDOL). Kept small to show client-visible fields.
    public class HoaDonCreateOLDto
    {
        public string? MaKH { get; set; }
        public string? GhiChu { get; set; }
        public decimal? TongTien { get; set; }
        // 1: TIENMAT, 2: ONLINE, 3: OCD
        public int? PhuongThucTT { get; set; }
        // optional order code from payment gateway
        public string? OrderCode { get; set; }
        public List<ChiTietHoaDonCreateOLDto>? Items { get; set; }
    }
}
