using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Dto
{
    public class GiaThuocDto
    {
        public string? MaGiaThuoc { get; set; } = null!;

        [Required]
        public string MaLoaiDonVi { get; set; } = null!;

        // number of base units (e.g. 1)
        public int SoLuong { get; set; } = 1;

        [Required]
        public decimal DonGia { get; set; }

        // whether this price row is active
        public bool TrangThai { get; set; } = true;
    }
}
