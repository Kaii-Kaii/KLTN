using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model;

namespace BE_QLTiemThuoc.Repositories
{
    public class MaRepository
    {
        private readonly AppDbContext _context;

        public MaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Ma?> GetByCodeAsync(string code)
        {
            return await _context.Mas
                .FirstOrDefaultAsync(m => m.Code == code);
        }

        public async Task<List<Ma>> SearchByCodeAsync(string code)
        {
            return await _context.Mas
                .Where(m => m.Code.Contains(code))
                .ToListAsync();
        }

        public async Task<Ma?> GetByMaAsync(string maCode)
        {
            return await _context.Mas
                .FirstOrDefaultAsync(m => m.MaCode == maCode);
        }

        public async Task<List<Ma>> GetAllAsync()
        {
            return await _context.Mas.ToListAsync();
        }
    }
}
