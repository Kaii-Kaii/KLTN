using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Thuoc;

namespace BE_QLTiemThuoc.Repositories
{
    public class NhaCungCapRepository
    {
        private readonly AppDbContext _context;

        public NhaCungCapRepository(AppDbContext context)
        {
            _context = context;
        }

        public AppDbContext Context => _context;

        public async Task<List<NhaCungCap>> GetAllAsync()
        {
            return await _context.NhaCungCaps.ToListAsync();
        }

        public async Task<NhaCungCap?> GetByIdAsync(string id)
        {
            return await _context.NhaCungCaps.FirstOrDefaultAsync(n => n.MaNCC == id);
        }

        public async Task CreateAsync(NhaCungCap ncc)
        {
            _context.NhaCungCaps.Add(ncc);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NhaCungCap ncc)
        {
            var entity = await _context.NhaCungCaps.FindAsync(ncc.MaNCC);
            if (entity == null) throw new Exception("Không tìm thấy nhà cung cấp.");
            _context.Entry(entity).CurrentValues.SetValues(ncc);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.NhaCungCaps.FindAsync(id);
            if (entity == null) throw new Exception("Không tìm thấy nhà cung cấp.");
            _context.NhaCungCaps.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
