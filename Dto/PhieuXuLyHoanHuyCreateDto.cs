using System.Collections.Generic;

namespace BE_QLTiemThuoc.Dto
{
    public class PhieuXuLyHoanHuyChiTietDto
    {
        public string MaLo { get; set; } = null!;
        public int SoLuong { get; set; }
        public bool? LoaiXuLy { get; set; }
    }

    public class PhieuXuLyHoanHuyCreateDto
    {
        public string MaNV { get; set; } = null!;
        // LoaiNguon: false = from kho (subtract inventory), true = from hoa don (do not subtract inventory)
        public bool? LoaiNguon { get; set; }
        // MaHD is optional and may be null when request originates from warehouse
        public string? MaHD { get; set; }
        public string? GhiChu { get; set; }
        public List<PhieuXuLyHoanHuyChiTietDto> ChiTiets { get; set; } = new List<PhieuXuLyHoanHuyChiTietDto>();
    }
}
