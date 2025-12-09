using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Dto;
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

        /// <summary>
        /// Lấy tất cả tên đăng nhập đã tồn tại
        /// </summary>
        public async Task<List<string>> GetAllUsernamesAsync()
        {
            return await _context.TaiKhoans
                .Where(x => x.TenDangNhap != null)
                .Select(x => x.TenDangNhap!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        /// <summary>
        /// Tạo nhân viên và tài khoản đăng nhập cùng lúc
        /// </summary>
        public async Task<NhanVienCreateResponseDto> CreateWithAccountAsync(NhanVienCreateDto createDto)
        {
            // Bắt đầu transaction để đảm bảo tính nhất quán
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kiểm tra xem username đã tồn tại chưa
                    var existingUsername = await _context.TaiKhoans
                        .FirstOrDefaultAsync(x => x.TenDangNhap == createDto.TenDangNhap);
                    
                    if (existingUsername != null)
                    {
                        throw new Exception($"Tên đăng nhập '{createDto.TenDangNhap}' đã được sử dụng");
                    }

                    // Tạo mã nhân viên tự động
                    string maNV = await GenerateStaffCodeAsync();

                    // Tạo entity NhanVien
                    var nhanVien = new NhanVien
                    {
                        MaNV = maNV,
                        HoTen = createDto.HoTen,
                        NgaySinh = (DateTime?)createDto.NgaySinh,
                        GioiTinh = createDto.GioiTinh,
                        DienThoai = createDto.DienThoai,
                        DiaChi = createDto.DiaChi,
                        ChucVu = (int?)createDto.ChucVu
                    };

                    // Thêm nhân viên vào database
                    _context.NhanViens.Add(nhanVien);

                    // Tạo tài khoản đăng nhập cho nhân viên
                    var taiKhoan = new TaiKhoan
                    {
                        MaTK = await GenerateAccountCodeAsync(),
                        TenDangNhap = createDto.TenDangNhap,
                        MatKhau = "123456", // Mật khẩu mặc định
                        VaiTro = createDto.ChucVu == 1 ? "Admin" : "NhanVien",
                        MaNV = maNV,
                        EMAIL = null,
                        ISEMAILCONFIRMED = 1,
                        OTP = null
                    };

                    _context.TaiKhoans.Add(taiKhoan);

                    // Lưu tất cả thay đổi
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    // Trả về response
                    return new NhanVienCreateResponseDto
                    {
                        MaNV = maNV,
                        HoTen = nhanVien.HoTen,
                        NgaySinh = nhanVien.NgaySinh,
                        GioiTinh = nhanVien.GioiTinh,
                        DiaChi = nhanVien.DiaChi,
                        DienThoai = nhanVien.DienThoai,
                        ChucVu = nhanVien.ChucVu,
                        TenDangNhap = taiKhoan.TenDangNhap,
                        ChucVuText = nhanVien.ChucVu == 1 ? "Admin" : "Nhân Viên",
                        NgayTao = DateTime.Now,
                        Message = $"Tạo nhân viên thành công! Tên đăng nhập: {taiKhoan.TenDangNhap}, Mật khẩu mặc định: 123456"
                    };
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    await transaction.RollbackAsync();
                    
                    // Log chi tiết lỗi
                    string errorDetails = ex.InnerException?.Message ?? ex.Message;
                    if (ex.InnerException?.InnerException != null)
                    {
                        errorDetails += " | " + ex.InnerException.InnerException.Message;
                    }
                    
                    throw new Exception($"Lỗi khi tạo nhân viên và tài khoản: {errorDetails}", ex);
                }
            }
        }

        /// <summary>
        /// Tạo nhân viên theo cách cũ (chỉ NhanVien model)
        /// </summary>
        
        public async Task UpdateAsync(NhanVien nhanVien)
        {
            await _repo.UpdateAsync(nhanVien);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<bool> DisableAccountAsync(string maNV)
        {
            try
            {
                // Kiểm tra nhân viên tồn tại
                var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(x => x.MaNV == maNV);
                if (nhanVien == null)
                    return false;

                // Tìm tài khoản liên kết qua MaNV
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaNV == maNV);
                if (taiKhoan == null)
                    return false;

                // Vô hiệu hoá tài khoản (set IsEmailConfirmed = 0)
                taiKhoan.ISEMAILCONFIRMED = 0;
                _context.TaiKhoans.Update(taiKhoan);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> EnableAccountAsync(string maNV)
        {
            try
            {
                // Kiểm tra nhân viên tồn tại
                var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(x => x.MaNV == maNV);
                if (nhanVien == null)
                    return false;

                // Tìm tài khoản liên kết qua MaNV
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaNV == maNV);
                if (taiKhoan == null)
                    return false;

                // Mở vô hiệu hoá tài khoản (set IsEmailConfirmed = 1)
                taiKhoan.ISEMAILCONFIRMED = 1;
                _context.TaiKhoans.Update(taiKhoan);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<object> GetAccountStatusAsync(string maNV)
        {
            try
            {
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaNV == maNV);
                if (taiKhoan == null)
                    return new { isEnabled = false, message = "Chưa có tài khoản" };

                // isEnabled = true nếu ISEMAILCONFIRMED = 1, false nếu = 0 hoặc null
                bool isEnabled = taiKhoan.ISEMAILCONFIRMED == 1;
                return new { isEnabled = isEnabled, status = taiKhoan.ISEMAILCONFIRMED };
            }
            catch (Exception)
            {
                return new { isEnabled = false, message = "Lỗi khi lấy trạng thái" };
            }
        }

        public async Task<TaiKhoan?> GetAccountInfoAsync(string maNV)
        {
            try
            {
                var taiKhoan = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.MaNV == maNV);
                return taiKhoan;
            }
            catch (Exception)
            {
                return null;
            }
        }
    

        private async Task<string> GenerateStaffCodeAsync()
        {
            // Lấy mã nhân viên cuối cùng từ database
            var lastStaff = await _context.NhanViens
                .OrderByDescending(x => x.MaNV)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastStaff != null && lastStaff.MaNV != null)
            {
                // Tách số từ mã nhân viên (VD: NV001 -> 001)
                if (lastStaff.MaNV.StartsWith("NV") && int.TryParse(lastStaff.MaNV.Substring(2), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"NV{nextNumber:D3}";
        }

        private async Task<string> GenerateAccountCodeAsync()
        {
            // Lấy mã tài khoản cuối cùng từ database
            var lastAccount = await _context.TaiKhoans
                .OrderByDescending(x => x.MaTK)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastAccount != null && lastAccount.MaTK != null)
            {
                // Tách số từ mã tài khoản (VD: TK0001 -> 0001)
                if (lastAccount.MaTK.StartsWith("TK") && int.TryParse(lastAccount.MaTK.Substring(2), out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"TK{nextNumber:D4}";
        }
    }
}
