namespace BE_QLTiemThuoc.DTOs
{
    public class CreateByPxhRequestDto
    {
        public string MaPXH { get; set; } = string.Empty;
        public string MaNV { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }
}
