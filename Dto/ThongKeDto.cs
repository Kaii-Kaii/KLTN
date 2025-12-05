namespace BE_QLTiemThuoc.Dto
{
    public class ThongKeDto
    {
        public string Label { get; set; } // Ngay, Thang, Nam
        public decimal Value { get; set; } // Tong tien
    }

    public class ThongKeResponse
    {
        public List<ThongKeDto> DoanhThu { get; set; } = new List<ThongKeDto>();
        public List<ThongKeDto> TienNhapHang { get; set; } = new List<ThongKeDto>();
        public List<ThongKeDto> TienHuyHang { get; set; } = new List<ThongKeDto>();
    }
}