using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Model.Kho;

namespace BE_QLTiemThuoc.Services
{
    public class ThuocViewService
    {
        private readonly AppDbContext _context;

        public ThuocViewService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<dynamic>> GetChuaTachLeAsync(int? status)
        {
            // status: null => return all (both SoLuongCon == 0 and >0)
            // status: 0 => return only SoLuongCon == 0
            // status: 1 => return only SoLuongCon > 0
            var today = DateTime.Now.Date;
            var q = from tk in _context.TonKhos
                  join t in _context.Thuoc on tk.MaThuoc equals t.MaThuoc
                  // When status == 0 (empty), include expired lots as well.
                  // For status == 1 keep only non-expired lots with SoLuongCon > 0.
                  // When status is not provided, keep previous behavior (exclude expired lots).
                  where tk.TrangThaiSeal == false && (
                       status == null ? true
                      : (status == 1 ? (tk.HanSuDung > today && tk.SoLuongCon > 0)
                      : (/* status == 0 */ tk.SoLuongCon == 0 || tk.HanSuDung <= today))
                  )
                  orderby tk.HanSuDung
                    select new
                    {
                        tk.MaLo,
                        tk.MaThuoc,
                        Code = t.Code,
                        TenThuoc = t.TenThuoc,
                        ThanhPhan = t.ThanhPhan,
                        MaLoaiThuoc = t.MaLoaiThuoc,
                        TenLoaiThuoc = _context.Set<LoaiThuoc>().Where(l => l.MaLoaiThuoc == t.MaLoaiThuoc).Select(l => l.TenLoaiThuoc).FirstOrDefault(),
                        DonViGoc = tk.MaLoaiDonViTinh,
                        TenLoaiDonViGoc = _context.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == tk.MaLoaiDonViTinh).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                        tk.SoLuongCon,
                        tk.HanSuDung,
                        tk.TrangThaiSeal,
                        tk.GhiChu
                    };

            return await q.ToListAsync<dynamic>();

        }

        public async Task<List<dynamic>> GetDaTachLeAsync(int? status)
        {
            // status: null => return all (both SoLuongCon == 0 and >0)
            // status: 0 => return only SoLuongCon == 0
            // status: 1 => return only SoLuongCon > 0
            var today = DateTime.Now.Date;
            var q = from tk in _context.TonKhos
                  join t in _context.Thuoc on tk.MaThuoc equals t.MaThuoc
                  // When status == 0 (empty), include expired lots as well.
                  // For status == 1 keep only non-expired lots with SoLuongCon > 0.
                  // When status is not provided, keep previous behavior (exclude expired lots).
                  where tk.TrangThaiSeal == true && (
                      status == null ? true
                      : (status == 1 ? (tk.HanSuDung > today && tk.SoLuongCon > 0)
                      : (/* status == 0 */ tk.SoLuongCon == 0 || tk.HanSuDung <= today))
                  )
                  orderby tk.HanSuDung
                    select new
                    {
                        tk.MaLo,
                        tk.MaThuoc,
                        Code = t.Code,
                        TenThuoc = t.TenThuoc,
                        MaLoaiThuoc = t.MaLoaiThuoc,
                        TenLoaiThuoc = _context.Set<LoaiThuoc>().Where(l => l.MaLoaiThuoc == t.MaLoaiThuoc).Select(l => l.TenLoaiThuoc).FirstOrDefault(),
                        TrangThai = "Đã tách lẻ",
                        DonViLe = tk.MaLoaiDonViTinh,
                        TenLoaiDonViLe = _context.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == tk.MaLoaiDonViTinh).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                        SoLuongConLe = tk.SoLuongCon,
                        tk.HanSuDung,
                        tk.TrangThaiSeal,
                        tk.GhiChu
                    };

            return await q.ToListAsync<dynamic>();
        }

        

        public async Task<List<dynamic>> GetTongSoLuongConAsync()
        {
            var q = from tk in _context.TonKhos
                    join t in _context.Thuoc on tk.MaThuoc equals t.MaThuoc
                    where tk.TrangThaiSeal == false && tk.SoLuongCon > 0
                    group tk by new { tk.MaThuoc, t.TenThuoc, t.Code, tk.MaLoaiDonViTinh } into g
                    orderby g.Key.TenThuoc
                    select new
                    {
                        MaThuoc = g.Key.MaThuoc,
                        Code = g.Key.Code,
                        TenThuoc = g.Key.TenThuoc,
                        DonViTinh = g.Key.MaLoaiDonViTinh,
                        TenLoaiDonVi = _context.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == g.Key.MaLoaiDonViTinh).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                        TongSoLuongCon = g.Sum(x => x.SoLuongCon)
                    };

            return await q.ToListAsync<dynamic>();
        }

        public async Task<List<dynamic>> GetSapHetHanAsync(int? days, int? months, int? years, DateTime? fromDate)
        {
            var now = DateTime.Now.Date;

            DateTime start;
            DateTime end;

            if (fromDate != null)
            {
                start = fromDate.Value.Date;
                end = now; 
                if (start > end)
                {
                    return new List<dynamic>();
                }
            }
            else if (days != null)
            {
                start = now;
                end = now.AddDays(days.Value < 0 ? 0 : days.Value);
            }
            else if (months != null)
            {
                start = now;
                end = now.AddMonths(months.Value < 0 ? 0 : months.Value);
            }
            else if (years != null)
            {
                start = now;
                end = now.AddYears(years.Value < 0 ? 0 : years.Value);
            }
            else
            {
                start = now;
                end = now.AddDays(7);
            }

                // include both upcoming expiries within the requested range AND any already-expired lots (HanSuDung < today)
                var today = DateTime.Now.Date;
                var q = from tk in _context.TonKhos
                    join t in _context.Thuoc on tk.MaThuoc equals t.MaThuoc
                    where tk.SoLuongCon > 0 && (
                      (tk.HanSuDung >= start && tk.HanSuDung <= end)  // upcoming within range
                      || (tk.HanSuDung < today)                         // already expired
                    )
                    orderby tk.HanSuDung
                    select new
                    {
                        tk.MaLo,
                        tk.MaThuoc,
                        Code = t.Code,
                        TenThuoc = t.TenThuoc,
                        DonViGoc = tk.MaLoaiDonViTinh,
                        TenLoaiDonViGoc = _context.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == tk.MaLoaiDonViTinh).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                        tk.SoLuongCon,
                        tk.HanSuDung,
                        tk.TrangThaiSeal,
                        tk.GhiChu,
                        NgayConLai = EF.Functions.DateDiffDay(now, tk.HanSuDung),
                        ThangConLai = EF.Functions.DateDiffMonth(now, tk.HanSuDung)
                    };

            return await q.ToListAsync<dynamic>();
        }

        // GET: LoDaHet - expired lots
        public async Task<List<dynamic>> GetLoDaHetAsync()
        {
            var today = DateTime.Now.Date;

            var q = from tk in _context.TonKhos
                    join t in _context.Thuoc on tk.MaThuoc equals t.MaThuoc
                    where tk.HanSuDung <= today
                    orderby tk.HanSuDung
                    select new
                    {
                        tk.MaLo,
                        tk.MaThuoc,
                        Code = t.Code,
                        TenThuoc = t.TenThuoc,
                        DonViGoc = tk.MaLoaiDonViTinh,
                        TenLoaiDonViGoc = _context.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == tk.MaLoaiDonViTinh).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                        tk.SoLuongCon,
                        tk.HanSuDung,
                        tk.TrangThaiSeal,
                        tk.GhiChu,
                        Status = 0
                    };

            return await q.ToListAsync<dynamic>();
        }

        public async Task<dynamic> GetLichSuLoAsync(string maLo)
        {
            if (string.IsNullOrWhiteSpace(maLo)) return new List<dynamic>();

#nullable disable
            // Phieu Nhap related history (existing behavior)
            var qPhieuNhap = from ct in _context.Set<ChiTietPhieuNhap>()
                             join pn in _context.PhieuNhaps on ct.MaPN equals pn.MaPN
                             join ncc in _context.NhaCungCaps on pn.MaNCC equals ncc.MaNCC into nccg
                             from ncc in nccg.DefaultIfEmpty()
                             join nv in _context.Set<NhanVien>() on pn.MaNV equals nv.MaNV into nvg
                             from nv in nvg.DefaultIfEmpty()
                             where ct.MaLo == maLo
                             select new
                             {
                                 Type = "PhieuNhap",
                                 EventDate = pn.NgayNhap,
                                 Phieu = new
                                 {
                                     pn.MaPN,
                                     pn.NgayNhap,
                                     pn.MaNCC,
                                     TenNCC = ncc != null ? ncc.TenNCC : null,
                                     pn.MaNV,
                                     TenNV = nv != null ? nv.HoTen : null,
                                     pn.TongTien,
                                     pn.GhiChu
                                 },
                                 ChiTiet = new
                                 {
                                     ct.MaCTPN,
                                     ct.MaPN,
                                     ct.MaLo,
                                     ct.MaThuoc,
                                     DonVi = ct.MaLoaiDonVi,
                                     SoLuongCon = _context.TonKhos.Where(x => x.MaLo == ct.MaLo).Select(x => x.SoLuongCon).FirstOrDefault(),
                                     HanSuDung = _context.TonKhos.Where(x => x.MaLo == ct.MaLo).Select(x => x.HanSuDung).FirstOrDefault(),
                                     OriginalSoLuong = ct.SoLuong,
                                     ct.DonGia,
                                     ct.ThanhTien,
                                     ct.MaLoaiDonVi,
                                     ct.GhiChu
                                 }
                             };

            // Phieu Quy Doi related history (include when maLo is source or destination)
            var listPN = await qPhieuNhap.ToListAsync<dynamic>();

            var listQD = new List<dynamic>();
            // Use raw SQL reader to fetch PHIEU_QUY_DOI + CT_PHIEU_QUY_DOI rows because there are no EF model classes for these tables
            var sql = @"
                SELECT pq.MaPhieuQD, pq.NgayQuyDoi, pq.MaNV, pq.GhiChu AS PhieuGhiChu,
                    ct.MaPhieuQD AS CtMaPhieuQD, ct.MaLoGoc, ct.MaLoMoi, ct.MaThuoc, ct.SoLuongQuyDoi, ct.TyLeQuyDoi, ct.SoLuongGoc, ct.MaLoaiDonViGoc, ct.MaLoaiDonViMoi
                FROM PHIEU_QUY_DOI pq
                JOIN CT_PHIEU_QUY_DOI ct ON ct.MaPhieuQD = pq.MaPhieuQD
                WHERE ct.MaLoGoc = @maLo OR ct.MaLoMoi = @maLo
                ";

            var conn = _context.Database.GetDbConnection();
            var openedHere = false;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                    openedHere = true;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@maLo";
                    p.Value = maLo;
                    cmd.Parameters.Add(p);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dynamic item = new System.Dynamic.ExpandoObject();
                            var dict = (IDictionary<string, object>)item;

                            dict["Type"] = "PhieuQuyDoi";
                            var ngay = reader["NgayQuyDoi"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["NgayQuyDoi"];
                            dict["EventDate"] = (object)(ngay ?? DateTime.MinValue);

                            dynamic phieu = new System.Dynamic.ExpandoObject();
                            var phieuDict = (IDictionary<string, object>)phieu;
                            phieuDict["MaPhieuQD"] = (object)(reader["MaPhieuQD"] == DBNull.Value ? null! : reader["MaPhieuQD"].ToString());
                            phieuDict["NgayQuyDoi"] = (object)(ngay ?? DateTime.MinValue);
                            phieuDict["MaNV"] = (object)(reader["MaNV"] == DBNull.Value ? null! : reader["MaNV"].ToString());
                            phieuDict["TenNV"] = (object)null!; // optional, could query NhanVien name if needed
                            phieuDict["GhiChu"] = (object)(reader["PhieuGhiChu"] == DBNull.Value ? null! : reader["PhieuGhiChu"].ToString());

                            dynamic chitiet = new System.Dynamic.ExpandoObject();
                            var ctDict = (IDictionary<string, object>)chitiet;
                            ctDict["MaPhieuQD"] = (object)(reader["CtMaPhieuQD"] == DBNull.Value ? null! : reader["CtMaPhieuQD"].ToString());
                            ctDict["MaLoGoc"] = (object)(reader["MaLoGoc"] == DBNull.Value ? null! : reader["MaLoGoc"].ToString());
                            ctDict["MaLoMoi"] = (object)(reader["MaLoMoi"] == DBNull.Value ? null! : reader["MaLoMoi"].ToString());
                            ctDict["MaThuoc"] = (object)(reader["MaThuoc"] == DBNull.Value ? null! : reader["MaThuoc"].ToString());
                            ctDict["SoLuongQuyDoi"] = (object)(reader["SoLuongQuyDoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoLuongQuyDoi"]));
                            ctDict["TyLeQuyDoi"] = (object)(reader["TyLeQuyDoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["TyLeQuyDoi"]));
                            ctDict["SoLuongGoc"] = (object)(reader["SoLuongGoc"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SoLuongGoc"]));
                            ctDict["MaLoaiDonViGoc"] = (object)(reader["MaLoaiDonViGoc"] == DBNull.Value ? null! : reader["MaLoaiDonViGoc"].ToString());
                            ctDict["MaLoaiDonViMoi"] = (object)(reader["MaLoaiDonViMoi"] == DBNull.Value ? null! : reader["MaLoaiDonViMoi"].ToString());

                            // (Defer TonKho enrichment until after the reader closes to avoid multiple active readers)

                            dict["Phieu"] = phieu;
                            dict["ChiTiet"] = chitiet;

                            listQD.Add(item);
                        }
                    }
                }
            }
            finally
            {
                if (openedHere && conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }

            // Enrich PhieuQuyDoi details with TonKho info (DonVi, SoLuongCon, HanSuDung)
            for (int i = 0; i < listQD.Count; i++)
            {
                dynamic item = listQD[i];
                var ct = (IDictionary<string, object>)item.ChiTiet;
                string maLoGoc = ct.ContainsKey("MaLoGoc") ? (ct["MaLoGoc"] as string ?? string.Empty) : string.Empty;
                string maLoMoi = ct.ContainsKey("MaLoMoi") ? (ct["MaLoMoi"] as string ?? string.Empty) : string.Empty;
                string lotOfInterest = maLoGoc == maLo ? maLoGoc : maLoMoi;

                if (!string.IsNullOrWhiteSpace(lotOfInterest))
                {
                    var tk = await _context.TonKhos.Where(t => t.MaLo == lotOfInterest)
                                                   .Select(t => new { t.MaLoaiDonViTinh, t.SoLuongCon, t.HanSuDung })
                                                   .FirstOrDefaultAsync();

                    string tenDonVi = tk != null ? _context.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == tk.MaLoaiDonViTinh).Select(d => d.TenLoaiDonVi).FirstOrDefault() : null;

                    ct["DonVi"] = tenDonVi;
                    ct["SoLuongCon"] = tk?.SoLuongCon;
                    ct["HanSuDung"] = tk?.HanSuDung;
                }
                else
                {
                    ct["DonVi"] = null;
                    ct["SoLuongCon"] = null;
                    ct["HanSuDung"] = null;
                }
            }

#nullable restore

            // Merge and order by event date descending
            var merged = listPN.Concat(listQD)
                .OrderByDescending(x => (DateTime)(x.EventDate ?? DateTime.MinValue))
                .ToList<dynamic>();

            // Fetch the current TonKho record for this maLo and include it as a separate 'Phieu' field
            var tonKho = await _context.TonKhos
                .Where(t => t.MaLo == maLo)
                .Select(t => new { t.MaLo, t.MaThuoc, t.MaLoaiDonViTinh, t.SoLuongNhap, t.SoLuongCon, t.HanSuDung, t.GhiChu })
                .FirstOrDefaultAsync();

            return new { Phieu = tonKho, LichSu = merged };
        }
    }
}