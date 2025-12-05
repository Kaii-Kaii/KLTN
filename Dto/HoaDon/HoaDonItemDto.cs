using System;

namespace BE_QLTiemThuoc.Dto
{
    // Shared DTO for invoice item operations: create (online), update placeholders, confirm allocations.
    public class HoaDonItemDto
    {
        public string? MaThuoc { get; set; }
        public string? DonVi { get; set; }

        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        public string? MaLD { get; set; }

        public DateTime? HanSuDung { get; set; }
    }
}
