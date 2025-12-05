using System;
using System.Collections.Generic;

namespace BE_QLTiemThuoc.Dto
{
    public class UpdateHoaDonDetailsDto
    {
        public string? MaHD { get; set; }

        public string? MaKH { get; set; }
        public string? MaNV { get; set; }
        public string? GhiChu { get; set; }
        public decimal? TongTien { get; set; }

        public List<UpdateChiTietHoaDonItemDto>? Items { get; set; }
    }

    public class UpdateChiTietHoaDonItemDto
    {
        public string? MaCTHD { get; set; }

        public DateTime? HanSuDung { get; set; }

        public string? MaLD { get; set; }
        public string? MaThuoc { get; set; }
        public string? DonVi { get; set; }
        public int? SoLuong { get; set; }
        public decimal? DonGia { get; set; }
    }
}
