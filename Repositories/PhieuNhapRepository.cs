using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Model.Thuoc;

namespace BE_QLTiemThuoc.Repositories
{
    public class PhieuNhapRepository
    {
        private readonly AppDbContext _context;

        public PhieuNhapRepository(AppDbContext context)
        {
            _context = context;
        }

        // Expose context for projections that are easier to write in service (keeps repository surface small)
        public AppDbContext Context => _context;

        public async Task<int> CountPhieuNhapsInMonthAsync(int year, int month)
        {
            return await _context.PhieuNhaps.Where(p => p.NgayNhap.Year == year && p.NgayNhap.Month == month).CountAsync();
        }

        public async Task AddPhieuNhapAsync(PhieuNhap pn)
        {
            await _context.PhieuNhaps.AddAsync(pn);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountChiTietByMaPNAsync(string maPN)
        {
            return await _context.Set<ChiTietPhieuNhap>().Where(c => c.MaPN == maPN).CountAsync();
        }

        public async Task AddChiTietRangeAsync(IEnumerable<ChiTietPhieuNhap> items)
        {
            await _context.Set<ChiTietPhieuNhap>().AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        public class UnitRow { public string? MaLoaiDonVi; public string? TenLoaiDonVi; }
        public async Task<List<UnitRow>> GetLoaiDonViByCodesAsync(List<string> codes)
        {
            if (codes == null || !codes.Any()) return new List<UnitRow>();
            return await _context.Set<LoaiDonVi>()
                .Where(ld => ld.MaLoaiDonVi != null && codes.Contains(ld.MaLoaiDonVi))
                .Select(ld => new UnitRow { MaLoaiDonVi = ld.MaLoaiDonVi, TenLoaiDonVi = ld.TenLoaiDonVi })
                .ToListAsync();
        }

        public async Task<List<TonKho>> GetExistingLotsAsync(List<string> maThuocSet, List<DateTime> hsdDates)
        {
            if (maThuocSet == null || !maThuocSet.Any() || hsdDates == null || !hsdDates.Any()) return new List<TonKho>();
            var query = _context.TonKhos.Where(t => maThuocSet.Contains(t.MaThuoc) && hsdDates.Contains(t.HanSuDung.Date));
            return await query.ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<PhieuNhap?> GetByIdAsync(string maPN)
        {
            return await _context.PhieuNhaps.FirstOrDefaultAsync(p => p.MaPN == maPN);
        }

        public async Task UpdatePhieuNhapAsync(PhieuNhap pn)
        {
            _context.PhieuNhaps.Update(pn);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePhieuNhapAsync(string maPN)
        {
            var phieuNhap = await _context.PhieuNhaps.FindAsync(maPN);
            if (phieuNhap != null)
            {
                _context.PhieuNhaps.Remove(phieuNhap);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
    }
}
