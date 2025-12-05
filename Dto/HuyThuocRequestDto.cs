using System.ComponentModel.DataAnnotations;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Dto
{
    public class HuyThuocRequestDto
    {
        [Required]
        public int LoaiHuy { get; set; } // 0 = KHO (hủy từ kho), 1 = HOADON (hủy từ hóa đơn)

        public HuyTuKhoDto? HuyTuKho { get; set; }
        public HuyTuHoaDonDto? HuyTuHoaDon { get; set; }
    }
}