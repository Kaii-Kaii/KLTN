using System;
using System.Collections.Generic;


namespace BE_QLTiemThuoc.Dto
{
    public class ConfirmOnlineDto
    {
        // Confirm an online invoice. Items are optional but when provided
        // the API will update MaLoaiDonVi (maLD) and HanSuDung for the invoice items
        // and then allocate lots accordingly.
        public string? MaHD { get; set; }
        public string? MaNV { get; set; }
        public string? GhiChu { get; set; }

        public List<ChiTietHoaDonCreateDto>? Items { get; set; }
            // Optional note to attach to the invoice when confirming
            
    }

}
