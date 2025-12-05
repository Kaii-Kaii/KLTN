using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Ban;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Repositories
{
 public class DanhGiaThuocRepository
 {
 private readonly AppDbContext _context;
 public DanhGiaThuocRepository(AppDbContext context)
 {
 _context = context;
 }

 public Task<DanhGiaThuoc?> GetByIdAsync(string maDanhGia) => _context.DanhGiaThuocs.FirstOrDefaultAsync(d => d.MaDanhGia == maDanhGia);
 public Task<DanhGiaThuoc?> GetByKhachHangAndThuocAsync(string maKh, string maThuoc) => _context.DanhGiaThuocs.FirstOrDefaultAsync(d => d.MaKH == maKh && d.MaThuoc == maThuoc);
 public async Task AddAsync(DanhGiaThuoc dg)
 {
 await _context.DanhGiaThuocs.AddAsync(dg);
 }
 public void Remove(DanhGiaThuoc dg)
 {
 _context.DanhGiaThuocs.Remove(dg);
 }
 public Task SaveChangesAsync() => _context.SaveChangesAsync();
 public Task<List<DanhGiaThuoc>> GetByThuocAsync(string maThuoc) => _context.DanhGiaThuocs.Where(d => d.MaThuoc == maThuoc).OrderByDescending(d=>d.NgayDanhGia).ToListAsync();
 public Task<List<DanhGiaThuoc>> GetByKhachHangAsync(string maKh) => _context.DanhGiaThuocs.Where(d => d.MaKH == maKh).OrderByDescending(d=>d.NgayDanhGia).ToListAsync();
 public Task<bool> HasCompletedOrderForThuocAsync(string maKh, string maThuoc)
 {
 // TrangThaiGiaoHang =3 => Completed
 return _context.HoaDons
 .Join(_context.ChiTietHoaDons,
 hd => hd.MaHD,
 ct => ct.MaHD,
 (hd, ct) => new { hd, ct })
 .AnyAsync(x => x.hd.MaKH == maKh && x.ct.MaThuoc == maThuoc && x.hd.TrangThaiGiaoHang ==3);
 }

 // New: danh sách thu?c khách có th? ?ánh giá (??n hoàn thành)
 public async Task<List<(string MaThuoc, string TenThuoc)>> GetEligibleThuocByKhachAsync(string maKh)
 {
 var query = from hd in _context.HoaDons
 join ct in _context.ChiTietHoaDons on hd.MaHD equals ct.MaHD
 join th in _context.Thuoc on ct.MaThuoc equals th.MaThuoc
 where hd.MaKH == maKh && hd.TrangThaiGiaoHang ==3 && ct.MaThuoc != null
 group th by new { th.MaThuoc, th.TenThuoc } into g
 select new { g.Key.MaThuoc, g.Key.TenThuoc };

 var list = await query.ToListAsync();
 return list.Select(x => (x.MaThuoc, x.TenThuoc)).ToList();
 }
 }
}
