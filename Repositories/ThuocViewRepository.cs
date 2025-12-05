using BE_QLTiemThuoc.Data;

namespace BE_QLTiemThuoc.Repositories
{
    // Minimal placeholder repository so existing DI registrations compile.
    // Keep implementation small; expand methods later if needed.
    public class ThuocViewRepository
    {
        private readonly AppDbContext _context;

        public ThuocViewRepository(AppDbContext context)
        {
            _context = context;
        }
    }
}
