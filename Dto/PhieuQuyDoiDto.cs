namespace BE_QLTiemThuoc.Dto
{
    public class PhieuQuyDoiItemDto
    {
        public string? MaThuoc { get; set; }
        public int SoLuongGoc { get; set; }
        public int? TyLeQuyDoi { get; set; }
        public int? SoLuongQuyDoi { get; set; }
        public string? MaLoaiDonViGoc { get; set; }
        public string? MaLoaiDonViMoi { get; set; }
        public DateTime? HanSuDungMoi { get; set; }
        public string? GhiChu { get; set; }
    }

    public class PhieuQuyDoiBatchCreateDto
    {
        public List<PhieuQuyDoiItemDto>? Items { get; set; }
        public string? MaNV { get; set; }
        public string? GhiChu { get; set; }
    }

    public class PhieuQuyDoiQuickByMaDto
    {
        public string MaThuoc { get; set; } = string.Empty;
        public string MaLoaiDonViMoi { get; set; } = string.Empty;
        public string? MaLoaiDonViGoc { get; set; }
        public DateTime? HanSuDungMoi { get; set; }
        public string? MaLoGoc { get; set; }
    }
    
}
