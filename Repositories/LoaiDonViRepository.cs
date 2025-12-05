using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Thuoc;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Repositories
{
    public class LoaiDonViRepository
    {
        private readonly AppDbContext _context;
        public LoaiDonViRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LoaiDonVi>> GetAllAsync()
        {
            return await _context.LoaiDonVi.OrderBy(x => x.TenLoaiDonVi).ToListAsync();
        }

        public async Task<LoaiDonVi?> GetByIdAsync(string ma)
        {
            if (string.IsNullOrWhiteSpace(ma)) return null;
            return await _context.LoaiDonVi.FirstOrDefaultAsync(x => x.MaLoaiDonVi == ma);
        }

        public async Task CreateAsync(LoaiDonVi item)
        {
            await _context.LoaiDonVi.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LoaiDonVi item)
        {
            _context.LoaiDonVi.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(LoaiDonVi item)
        {
            _context.LoaiDonVi.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
