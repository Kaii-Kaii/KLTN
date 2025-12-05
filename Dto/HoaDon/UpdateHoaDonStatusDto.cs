namespace BE_QLTiemThuoc.Dto
{
    public class UpdateHoaDonStatusDto
    {
        public string? MaHD { get; set; }
        public int TrangThaiGiaoHang { get; set; }
        // Optional: update invoice's assigned employee when provided
        public string? MaNV { get; set; }
    }
}
