using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Services
{
    public class NhanVienService
    {
        private readonly NhanVienRepository _repo;
        private readonly AppDbContext _context;

        public NhanVienService(NhanVienRepository repo, AppDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<List<NhanVien>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<NhanVien?> GetByIdAsync(string id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<NhanVien?> CreateAsync(NhanVien nhanVien)
        {
            // Tạo mã nhân viên tự động
            if (string.IsNullOrEmpty(nhanVien.MaNV))
            {
                nhanVien.MaNV = GenerateStaffCode();
            }

            // Thêm nhân viên vào database
            await _repo.AddAsync(nhanVien);

            // Tạo tài khoản tự động cho nhân viên
            var taiKhoan = new TaiKhoan
            {
                MaTK = GenerateAccountCode(),
                TenDangNhap = nhanVien.MaNV, // Username = Mã NV
                MatKhau = "123456", // Default password
                EMAIL = nhanVien.MaNV + "@pharmacy.local", // Default email
                ISEMAILCONFIRMED = 0,
                KhachHang = null
            };

            _context.TaiKhoans.Add(taiKhoan);
            await _context.SaveChangesAsync();

            return nhanVien;
        }

        public async Task UpdateAsync(NhanVien nhanVien)
        {
            await _repo.UpdateAsync(nhanVien);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            return await _repo.DeleteAsync(id);
        }

        private string GenerateStaffCode()
        {
            // Format: NV + timestamp or random
            return $"NV{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string GenerateAccountCode()
        {
            // Format: TK + timestamp or random
            return $"TK{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}