using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Dto
{
    public class PhieuHuyDto
    {
        // Optional: client may provide MaPH when updating; server may generate when creating
        public string? MaPH { get; set; }

        // Optional NgayHuy (server will default to now when creating if not provided)
        public DateTime? NgayHuy { get; set; }
        [Required]
        public string MaNV { get; set; } = null!;

        [Required]
        public bool LoaiHuy { get; set; } // -- false: KHO (hủy từ kho), true: HOADON (hủy từ hóa đơn)

        public string? MaHD { get; set; } // Mã hóa đơn (nếu hủy từ hóa đơn)

        // Lý do hủy (theo DDL: LyDoHuy NVARCHAR(1000))
        public string? LyDoHuy { get; set; }

        // Aggregates (optional input; server can compute)
        public int? TongMatHangHuy { get; set; }
        public int? TongSoLuongHuy { get; set; }
        public decimal? TongTienHuy { get; set; }
        public decimal? TongTienKho { get; set; }
        public decimal? TongTien { get; set; }

        [Required]
        public List<ChiTietPhieuHuyDto> ChiTietPhieuHuys { get; set; } = new List<ChiTietPhieuHuyDto>();
    }

    public class ChiTietPhieuHuyDto
    {
        // Optional when creating: server may generate MaCTPH
        public string? MaCTPH { get; set; }
        [Required]
        public string MaLo { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng hủy phải lớn hơn 0")]
        public int SoLuongHuy { get; set; }

        // Đơn giá (tùy chọn) để tính ThanhTien khi cập nhật/nhập liệu
        public decimal? DonGia { get; set; }

        // Thành tiền (có thể gửi lên, hoặc server sẽ tính = SoLuongHuy * DonGia)
        public decimal? ThanhTien { get; set; }

        public bool? LoaiHuy { get; set; } //-- true: huỷ vào kho, false: huỷ bình thường

        public string? GhiChu { get; set; }

    }

    // DTO mới cho việc hủy hóa đơn với xử lý linh hoạt
    public class HuyHoaDonDto
    {
        [Required]
        public string MaHD { get; set; } = null!;

        [Required]
        public string MaNV { get; set; } = null!;

        public string? GhiChu { get; set; }

        [Required]
        public List<XuLyThuocTrongHoaDonDto> XuLyThuocs { get; set; } = new List<XuLyThuocTrongHoaDonDto>();
    }

    public class XuLyThuocTrongHoaDonDto
    {
        [Required]
        public string MaLo { get; set; } = null!;

        // Optional compatibility field: indicates whether to return to stock (true) or normal cancel (false)
        public bool? LoaiHuy { get; set; } //--1: huỷ vào kho  0: huỷ bình thường

        // New: explicit processing type when client prefers string values
        // Allowed: "HUY" or "HOAN_LAI". If provided, takes precedence over LoaiHuy.
        public string? LoaiXuLy { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int? SoLuongHuy { get; set; } // Chỉ cần khi LoaiXuLy = "HUY"

        public string? GhiChu { get; set; }
    }

    // DTO cho hủy từ kho
    public class HuyTuKhoDto
    {
        [Required]
        public string MaNhanVien { get; set; } = null!;

        [Required]
        [StringLength(1000, ErrorMessage = "Lý do hủy không được vượt quá 1000 ký tự")]
        public string LyDoHuy { get; set; } = null!;

        [Required]
        public List<ChiTietHuyTuKhoDto> ChiTietPhieuHuy { get; set; } = new List<ChiTietHuyTuKhoDto>();
    }

    public class ChiTietHuyTuKhoDto
    {
        [Required]
        public string MaLo { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng hủy phải lớn hơn 0")]
        public int SoLuongHuy { get; set; }

        [StringLength(500, ErrorMessage = "Lý do chi tiết không được vượt quá 500 ký tự")]
        public string? LyDoChiTiet { get; set; }
    }

    // DTO cho hủy từ hóa đơn
    public class HuyTuHoaDonDto
    {
        [Required]
        public string MaHoaDon { get; set; } = null!;

        [Required]
        public string MaNhanVien { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Lý do hủy không được vượt quá 1000 ký tự")]
        public string? LyDoHuy { get; set; }

        [Required]
        public List<ChiTietXuLyHoaDonDto> ChiTietXuLy { get; set; } = new List<ChiTietXuLyHoaDonDto>();
    }

    public class ChiTietXuLyHoaDonDto
    {
        [Required]
        public string MaLo { get; set; } = null!;

        [Required]
        public string? LoaiXuLy { get; set; } //--1: huỷ vào kho  0: huỷ bình thường

        [Required]

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng hủy phải lớn hơn 0")]
        public int? SoLuongHuy { get; set; } // Chỉ cần khi LoaiXuLy = "HUY"

        [StringLength(500, ErrorMessage = "Lý do chi tiết không được vượt quá 500 ký tự")]
        public string? LyDoChiTiet { get; set; } // Lý do cho việc hủy hoặc hoàn lại

        public string? GhiChu { get; set; }
    }
}