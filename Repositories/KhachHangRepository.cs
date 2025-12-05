using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model;

namespace BE_QLTiemThuoc.Repositories
{
    public class KhachHangRepository
    {
        private readonly AppDbContext _context;

        public KhachHangRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<KhachHang>> GetAllAsync()
        {
            return await _context.KhachHangs.ToListAsync();
        }

        public async Task AddAsync(KhachHang kh)
        {
            _context.KhachHangs.Add(kh);
            await _context.SaveChangesAsync();
        }

        public KhachHang? GetLastAccount()
        {
            return _context.KhachHangs
                .OrderByDescending(t => t.MAKH)
                .FirstOrDefault();
        }

        public async Task<KhachHang?> GetByIdAsync(string maKhachHang)
        {
            return await _context.KhachHangs.FirstOrDefaultAsync(k => k.MAKH == maKhachHang);
        }

        public async Task UpdateAsync(KhachHang khachHang)
        {
            _context.KhachHangs.Update(khachHang);
            await _context.SaveChangesAsync();
        }
    }
}