using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model;

namespace BE_QLTiemThuoc.Repositories
{
    public class NhanVienRepository
    {
        private readonly AppDbContext _context;

        public NhanVienRepository(AppDbContext context)
        {
            _context = context;
        }

        public AppDbContext Context => _context;

        public async Task<List<NhanVien>> GetAllAsync()
        {
            return await _context.NhanViens.ToListAsync();
        }

        public async Task<NhanVien?> GetByIdAsync(string id)
        {
            return await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNV == id);
        }

        public async Task CreateAsync(NhanVien nv)
        {
            _context.NhanViens.Add(nv);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(NhanVien nv)
        {
            _context.NhanViens.Add(nv);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NhanVien nv)
        {
            var entity = await _context.NhanViens.FindAsync(nv.MaNV);
            if (entity == null) throw new Exception("Không tìm thấy nhân viên.");
            _context.Entry(entity).CurrentValues.SetValues(nv);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) throw new Exception("Không tìm thấy nhân viên.");
            _context.NhanViens.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}