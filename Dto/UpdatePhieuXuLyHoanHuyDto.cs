namespace BE_QLTiemThuoc.Dto
{
    public class UpdatePhieuXuLyHoanHuyDto
    {
        public string? MaPXH { get; set; }
        public string? GhiChu { get; set; }
        public string? MaNV_Duyet { get; set; }
        public List<UpdateChiTietPhieuXuLyDto>? ChiTiets { get; set; }
    }

    public class UpdateChiTietPhieuXuLyDto
    {
        public string? MaCTPXH { get; set; }
        public int? SoLuong { get; set; }
        public bool? LoaiXuLy { get; set; }
    }
}