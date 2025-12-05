using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Kho;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using BE_QLTiemThuoc.Model.Thuoc;
namespace BE_QLTiemThuoc.Repositories
{
    public class PhieuXuLyHoanHuyRepository
    {
        private readonly AppDbContext _context;

        public PhieuXuLyHoanHuyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PhieuXuLyHoanHuy>> GetAllAsync(DateTime? start = null, DateTime? end = null, string? maNV_Tao = null, bool? loaiNguon = null, int? trangThai = null)
        {
            var q = _context.PhieuXuLyHoanHuys
                .AsNoTracking()
                .AsQueryable();

            if (start.HasValue)
            {
                var s = start.Value.Date;
                q = q.Where(p => p.NgayTao.HasValue && p.NgayTao.Value >= s);
            }
            if (end.HasValue)
            {
                var e = end.Value.Date.AddDays(1).AddTicks(-1);
                q = q.Where(p => p.NgayTao.HasValue && p.NgayTao.Value <= e);
            }

            if (!string.IsNullOrWhiteSpace(maNV_Tao))
            {
                q = q.Where(p => p.MaNV_Tao == maNV_Tao);
            }

            if (loaiNguon.HasValue)
            {
                q = q.Where(p => p.LoaiNguon.HasValue && p.LoaiNguon.Value == loaiNguon.Value);
            }

            if (trangThai.HasValue)
            {
                q = q.Where(p => p.TrangThai.HasValue && p.TrangThai.Value == trangThai.Value);
            }

            // also include any PhieuXuLyHoanHuy that reference expired TonKho (HanSuDung < today)
            var today = DateTime.Today;
            var expiredPxhQuery = _context.Set<ChiTietPhieuXuLy>()
                .Join(_context.TonKhos, ct => ct.MaLo, tk => tk.MaLo, (ct, tk) => new { ct.MaPXH, tk.HanSuDung })
                .Where(x => x.HanSuDung < today)
                .Select(x => x.MaPXH)
                .Distinct();

            var expiredPhieuQuery = _context.PhieuXuLyHoanHuys
                .AsNoTracking()
                .Where(p => expiredPxhQuery.Contains(p.MaPXH));

            // Apply identity/status filters to expired results as well (but ignore date range)
            if (!string.IsNullOrWhiteSpace(maNV_Tao))
            {
                expiredPhieuQuery = expiredPhieuQuery.Where(p => p.MaNV_Tao == maNV_Tao);
            }
            if (loaiNguon.HasValue)
            {
                expiredPhieuQuery = expiredPhieuQuery.Where(p => p.LoaiNguon.HasValue && p.LoaiNguon.Value == loaiNguon.Value);
            }
            if (trangThai.HasValue)
            {
                expiredPhieuQuery = expiredPhieuQuery.Where(p => p.TrangThai.HasValue && p.TrangThai.Value == trangThai.Value);
            }

            var finalQuery = q.Union(expiredPhieuQuery).Distinct().OrderByDescending(p => p.NgayTao);

            return await finalQuery.ToListAsync();
        }

        public async Task<dynamic?> GetDetailsByMaAsync(string maPXH)
        {
            if (string.IsNullOrWhiteSpace(maPXH)) return null;

            var header = await _context.PhieuXuLyHoanHuys
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPXH == maPXH);

            if (header == null) return null;

            // Join to TonKho -> Thuoc -> LoaiDonVi to enrich detail rows
            var ctList = await _context.Set<ChiTietPhieuXuLy>()
                .AsNoTracking()
                .Where(ct => ct.MaPXH == maPXH)
                .GroupJoin(
                    _context.TonKhos.AsNoTracking(),
                    ct => ct.MaLo,
                    tk => tk.MaLo,
                    (ct, tks) => new { ct, tks }
                )
                .SelectMany(x => x.tks.DefaultIfEmpty(), (x, tk) => new { x.ct, tk })
                .GroupJoin(
                    _context.Set<Thuoc>().AsNoTracking(),
                    x => x.tk != null ? x.tk.MaThuoc : null,
                    th => th.MaThuoc,
                    (x, ths) => new { x.ct, x.tk, ths }
                )
                .SelectMany(x => x.ths.DefaultIfEmpty(), (x, th) => new { x.ct, x.tk, th })
                .GroupJoin(
                    _context.Set<LoaiDonVi>().AsNoTracking(),
                    x => x.tk != null ? x.tk.MaLoaiDonViTinh : null,
                    ldv => ldv.MaLoaiDonVi,
                    (x, ldvs) => new { x.ct, x.tk, x.th, ldvs }
                )
                .SelectMany(x => x.ldvs.DefaultIfEmpty(), (x, ldv) => new
                {
                    x.ct.MaCTPXH,
                    x.ct.MaPXH,
                    x.ct.MaLo,
                    x.ct.SoLuong,
                    x.ct.LoaiXuLy,
                    MaThuoc = x.tk != null ? x.tk.MaThuoc : null,
                    TenThuoc = x.th != null ? x.th.TenThuoc : null,
                    MaLoaiDonVi = x.tk != null ? x.tk.MaLoaiDonViTinh : null,
                    TenLoaiDonVi = ldv != null ? ldv.TenLoaiDonVi : null
                })
                .OrderBy(r => r.MaCTPXH)
                .ToListAsync();

            dynamic outObj = new System.Dynamic.ExpandoObject();
            var od = (IDictionary<string, object>)outObj;
            od["Phieu"] = header;
            od["ChiTiets"] = ctList;
            return outObj;
        }
    }
}