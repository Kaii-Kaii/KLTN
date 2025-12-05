using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Repositories;

namespace BE_QLTiemThuoc.Services
{
    public class KhachHangService
    {
        private readonly KhachHangRepository _repo;

        public KhachHangService(KhachHangRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<KhachHang>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<KhachHang> CreateAsync(KhachHang dto)
        {
            // generate MAKH
            var last = _repo.GetLastAccount();
            string lastCode = last?.MAKH ?? "KH0000";
            int number = 1;
            try { number = int.Parse(lastCode.Substring(2)) + 1; } catch { number = 1; }
            var newMaKH = "KH" + number.ToString("D4");

            var kh = new KhachHang
            {
                MAKH = newMaKH,
                HoTen = dto.HoTen,
                NgaySinh = dto.NgaySinh,
                DienThoai = dto.DienThoai,
                GioiTinh = dto.GioiTinh,
                DiaChi = dto.DiaChi
            };

            await _repo.AddAsync(kh);
            return kh;
        }

        public async Task<KhachHang?> GetByIdAsync(string maKhachHang)
        {
            return await _repo.GetByIdAsync(maKhachHang);
        }

        public async Task<KhachHang?> UpdateAsync(string maKhachHang, KhachHang dto)
        {
            var existing = await _repo.GetByIdAsync(maKhachHang);
            if (existing == null) return null;

            // Update fields
            existing.HoTen = dto.HoTen;
            existing.NgaySinh = dto.NgaySinh;
            existing.DienThoai = dto.DienThoai;
            existing.GioiTinh = dto.GioiTinh;
            existing.DiaChi = dto.DiaChi;

            await _repo.UpdateAsync(existing);
            return existing;
        }
    }
}
