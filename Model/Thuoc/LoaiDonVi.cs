using System.ComponentModel.DataAnnotations;

namespace BE_QLTiemThuoc.Model.Thuoc
{
    public class LoaiDonVi
    {
    [Key]
    public string? MaLoaiDonVi { get; set; }

    public string? TenLoaiDonVi { get; set; }
    }
}
