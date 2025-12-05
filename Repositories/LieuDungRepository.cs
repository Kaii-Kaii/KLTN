using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Repositories
{
    public class LieuDungRepository
    {
        private readonly AppDbContext _context;

        public LieuDungRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LieuDung>> GetAllAsync()
        {
            return await _context.Set<LieuDung>()
                .Select(ld => new LieuDung { MaLD = ld.MaLD, TenLieuDung = ld.TenLieuDung })
                .ToListAsync();
        }

        public async Task<LieuDung?> GetByIdAsync(string maLD)
        {
            return await _context.Set<LieuDung>().FirstOrDefaultAsync(ld => ld.MaLD == maLD);
        }

        public async Task AddAsync(LieuDung lieuDung)
        {
            _context.Set<LieuDung>().Add(lieuDung);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LieuDung lieuDung)
        {
            _context.Set<LieuDung>().Update(lieuDung);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string maLD)
        {
            var lieuDung = await _context.Set<LieuDung>().FirstOrDefaultAsync(ld => ld.MaLD == maLD);
            if (lieuDung != null)
            {
                _context.Set<LieuDung>().Remove(lieuDung);
                await _context.SaveChangesAsync();
            }
        }
    }
}