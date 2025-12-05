using System.Collections.Generic;

namespace BE_QLTiemThuoc.Dto
{
    public class LoaiItemDto
    {
        public string? MaLoaiThuoc { get; set; }
        public string? TenLoaiThuoc { get; set; }
    }

    public class GroupWithLoaiDto
    {
        public string? MaNhomLoai { get; set; }
        public string? TenNhomLoai { get; set; }
        public List<LoaiItemDto> Loai { get; set; } = new List<LoaiItemDto>();
    }
}
