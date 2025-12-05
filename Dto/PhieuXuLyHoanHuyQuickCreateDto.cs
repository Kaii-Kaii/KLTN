namespace BE_QLTiemThuoc.Dto
{
    public class PhieuXuLyHoanHuyQuickCreateDto
    {
        public string MaNV { get; set; } = null!;
        public string MaThuoc { get; set; } = null!;
        public string MaLoaiDonVi { get; set; } = null!;
        public int SoLuong { get; set; }
        public string? LyDo { get; set; }
    }
}
