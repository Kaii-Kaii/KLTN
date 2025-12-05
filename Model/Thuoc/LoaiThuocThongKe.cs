namespace BE_QLTiemThuoc.Model.Thuoc
{
    public class LoaiThuocThongKe
    {
        public string MaLoaiThuoc { get; set; } = null!;
        public string TenLoaiThuoc { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string? MaNhomLoai { get; set; }
        public string? TenNhomLoai { get; set; }
        public int SoLuongThuoc { get; set; }
    }
}
