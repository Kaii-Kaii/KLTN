using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Thuoc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Repositories
{
    // Concrete repository (no interface) — contains DB access helpers for Thuoc
    public class ThuocRepository
    {
        private readonly AppDbContext _context;

        public ThuocRepository(AppDbContext context)
        {
            _context = context;
        }

        // Expose the DbContext for complex projections (pragmatic migration choice)
        public AppDbContext Context => _context;

        public async Task AddAsync(Thuoc thuoc)
        {
            await _context.Thuoc.AddAsync(thuoc);
        }

        public Task<Thuoc?> FindAsync(string id)
        {
            return _context.Thuoc.FindAsync(id).AsTask();
        }

        public void Remove(Thuoc thuoc)
        {
            _context.Thuoc.Remove(thuoc);
        }

        public Task<bool> AnyByMaThuocAsync(string maThuoc)
        {
            return _context.Thuoc.AnyAsync(t => t.MaThuoc == maThuoc);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
