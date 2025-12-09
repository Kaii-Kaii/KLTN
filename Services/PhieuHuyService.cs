using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Thuoc;
namespace BE_QLTiemThuoc.Services
{
    public class PhieuHuyService
    {
        private readonly AppDbContext _context;

        public PhieuHuyService(AppDbContext context)
        {
            _context = context;
        }

        
        /// <summary>
        /// Given a MaLo (lot code), trace and return the originating PhieuNhap(s).
        /// Algorithm:
        /// 1) If there is a ChiTietPhieuNhap with MaLo == maLo, return its MaPN.
        /// 2) Otherwise, look up CT_PHIEU_QUY_DOI rows where MaLoMoi == maLo to find MaLoGoc and the MaPhieuQD that produced it.
        ///    Repeat step 1 for each MaLoGoc. If MaLoGoc itself is not found in ChiTietPhieuNhap, repeat the CT_PHIEU_QUY_DOI lookup
        ///    up to a configurable depth (default 2 conversion levels) to find the original MaPN.
        /// Returns a list of trace results (may be multiple if lot originated from multiple source lots).
        /// </summary>
        public async Task<List<object>> FindOriginalPhieuNhapsByMaLoAsync(string maLo, int maxConversionDepth = 0)
        {
            if (string.IsNullOrWhiteSpace(maLo)) throw new ArgumentException("maLo is required");

            var ctx = _context;
            // Defensive check: ensure the underlying DbConnection has a connection string.
            // If this is empty it usually means configuration/environment wasn't loaded
            // before registering DbContext (or the launch profile overridden it with an empty value).
            var runtimeConn = ctx.Database.GetDbConnection();
            if (runtimeConn == null || string.IsNullOrWhiteSpace(runtimeConn.ConnectionString))
            {
                throw new InvalidOperationException("Database connection is not configured. Ensure 'DefaultConnection' is set in appsettings or provided via environment (.env) and Env.Load() runs before DbContext registration.");
            }
            var results = new List<object>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Normalize input
            string Normalize(string s) => s?.Trim() ?? string.Empty;

            // BFS queue: tuple of (currentMaLo, viaPhieuQD)
            var queue = new Queue<(string MaLo, string? ViaPhieuQD)>();
            queue.Enqueue((Normalize(maLo), null));

            using (var conn = ctx.Database.GetDbConnection())
            {
                await conn.OpenAsync();

                while (queue.Any())
                {
                    var (currentLoRaw, viaPhieuQD) = queue.Dequeue();
                    var currentLo = Normalize(currentLoRaw);
                    if (string.IsNullOrEmpty(currentLo) || visited.Contains(currentLo)) continue;
                    visited.Add(currentLo);

                    // 1) Check ChiTietPhieuNhap (case-insensitive, trimmed)
                    var ct = await ctx.ChiTietPhieuNhaps.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.MaLo != null && c.MaLo.Trim().ToLower() == currentLo.ToLower());

                    if (ct != null)
                    {
                        results.Add(new
                        {
                            MaLo = currentLo,
                            FoundIn = "ChiTietPhieuNhap",
                            MaCTPN = ct.MaCTPN,
                            MaPN = ct.MaPN,
                            ViaPhieuQD = viaPhieuQD
                        });
                        // once found, do not traverse further from this branch
                        continue;
                    }

                    // 2) Not found in ChiTietPhieuNhap -> look up CT_PHIEU_QUY_DOI where MaLoMoi = currentLo
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT MaPhieuQD, MaLoGoc FROM CT_PHIEU_QUY_DOI WHERE LTRIM(RTRIM(MaLoMoi)) = @maLo";
                        var p = cmd.CreateParameter();
                        p.ParameterName = "@maLo";
                        p.Value = currentLo;
                        cmd.Parameters.Add(p);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var foundAny = false;
                            while (await reader.ReadAsync())
                            {
                                foundAny = true;
                                var maPhieuQD = reader["MaPhieuQD"] == DBNull.Value ? null : reader["MaPhieuQD"].ToString();
                                var maLoGoc = reader["MaLoGoc"] == DBNull.Value ? null : reader["MaLoGoc"].ToString();

                                results.Add(new
                                {
                                    MaLo = currentLo,
                                    FoundIn = "CT_PHIEU_QUY_DOI",
                                    MaPhieuQD = maPhieuQD,
                                    MaLoGoc = maLoGoc?.Trim(),
                                    ViaPhieuQD = viaPhieuQD
                                });

                                // Enqueue source lot for further tracing
                                if (!string.IsNullOrWhiteSpace(maLoGoc))
                                {
                                    queue.Enqueue((Normalize(maLoGoc), maPhieuQD));
                                }
                            }

                            if (!foundAny)
                            {
                                // nothing produced this lot (no CT_PHIEU_QUY_DOI with MaLoMoi)
                                results.Add(new { MaLo = currentLo, FoundIn = "Unknown", ViaPhieuQD = viaPhieuQD });
                            }
                        }
                    }
                }
            }

            return results;
        }
        private string GenerateMaPhieuHuy()
        {
            return "PH" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private string GenerateMaChiTietPhieuHuy()
        {
            // Fit within VARCHAR(20): prefix(4) + yyMMddHHmmssfff(15) = 19 chars
            return "CTPH" + DateTime.Now.ToString("yyMMddHHmmssfff");
        }

        // Tìm đơn giá theo lô với quy tắc:
        // 1) Ưu tiên tìm trong ChiTietPhieuNhap theo MaLo; nếu có, lấy DonGia gốc
        // 2) Nếu không có, tra bảng CT_PHIEU_QUY_DOI để tìm MaLoGoc rồi quay lại bước 1
        // 3) Nếu đơn vị của lô hiện tại khác đơn vị gốc, quy đổi đơn giá theo tỉ lệ SoLuong trong GiaThuoc:
        //    DonGiaQuyDoi = DonGiaGoc * (SoLuong_goc / SoLuong_donvi_hien_tai)
        private async Task<decimal> GetDonGiaByMaLoAsync(string maLo, string maThuoc, string maLoaiDonViHienTai)
        {
            string Normalize(string s) => s?.Trim() ?? string.Empty;
            maLo = Normalize(maLo);
            maThuoc = Normalize(maThuoc);
            maLoaiDonViHienTai = Normalize(maLoaiDonViHienTai);

            // Try direct in CTPN
            var ct = await _context.ChiTietPhieuNhaps.AsNoTracking()
                .FirstOrDefaultAsync(c => c.MaLo != null && c.MaLo.Trim().ToLower() == maLo.ToLower());

            string? maLoaiDonViGoc = null;
            string? maThuocGoc = null;
            decimal donGiaGoc = 0m;

            if (ct != null)
            {
                donGiaGoc = ct.DonGia;
                var tkGoc = await _context.TonKhos.AsNoTracking().FirstOrDefaultAsync(t => t.MaLo == maLo);
                maLoaiDonViGoc = tkGoc?.MaLoaiDonViTinh;
                maThuocGoc = tkGoc?.MaThuoc;
            }
            else
            {
                // Lookup conversion to find source lot using the existing EF connection
                var conn = _context.Database.GetDbConnection();
                var openedHere = false;
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                    openedHere = true;
                }
                try
                {
                    using var cmd = conn.CreateCommand();
                    // Attach to ambient EF transaction if present to satisfy SQL Server requirements
                    var efTx = _context.Database.CurrentTransaction;
                    if (efTx != null)
                    {
                        cmd.Transaction = efTx.GetDbTransaction();
                    }
                    cmd.CommandText = "SELECT TOP 1 MaLoGoc FROM CT_PHIEU_QUY_DOI WHERE LTRIM(RTRIM(MaLoMoi)) = @maLo";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@maLo";
                    p.Value = maLo;
                    cmd.Parameters.Add(p);
                    string? maLoGoc = null;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            maLoGoc = reader["MaLoGoc"] == DBNull.Value ? null : reader["MaLoGoc"].ToString();
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(maLoGoc))
                    {
                        var ctGoc = await _context.ChiTietPhieuNhaps.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.MaLo != null && c.MaLo.Trim().ToLower() == maLoGoc.Trim().ToLower());
                        if (ctGoc != null)
                        {
                            donGiaGoc = ctGoc.DonGia;
                            var tkGoc2 = await _context.TonKhos.AsNoTracking().FirstOrDefaultAsync(t => t.MaLo == maLoGoc);
                            maLoaiDonViGoc = tkGoc2?.MaLoaiDonViTinh;
                            maThuocGoc = tkGoc2?.MaThuoc;
                        }
                    }
                }
                finally
                {
                    if (openedHere)
                    {
                        await conn.CloseAsync();
                    }
                }
            }

            if (donGiaGoc <= 0m) return 0m;

            if (string.Equals(maLoaiDonViGoc, maLoaiDonViHienTai, StringComparison.OrdinalIgnoreCase))
            {
                return donGiaGoc;
            }

            // Unit conversion via GiaThuoc ratios
            var maThuocForRatio = string.IsNullOrWhiteSpace(maThuocGoc) ? maThuoc : maThuocGoc;
            var giaList = await _context.GiaThuocs.AsNoTracking()
                .Where(g => g.MaThuoc == maThuocForRatio)
                .Select(g => new { g.MaLoaiDonVi, g.SoLuong })
                .ToListAsync();
            var soLuongGoc = giaList.FirstOrDefault(x => x.MaLoaiDonVi == maLoaiDonViGoc)?.SoLuong ?? 0;
            var soLuongHienTai = giaList.FirstOrDefault(x => x.MaLoaiDonVi == maLoaiDonViHienTai)?.SoLuong ?? 0;

            if (soLuongGoc <= 0 || soLuongHienTai <= 0)
            {
                return donGiaGoc; // fallback: cannot convert without ratios
            }

            var donGiaQuyDoi = donGiaGoc * ((decimal)soLuongGoc / (decimal)soLuongHienTai);
            return donGiaQuyDoi;
        }

        public async Task<List<dynamic>> GetAllAsync(DateTime? start = null, DateTime? end = null, string? maNV = null)
        {
            var query = _context.PhieuHuys.AsNoTracking().AsQueryable();
            if (start.HasValue)
            {
                query = query.Where(p => p.NgayHuy >= start.Value);
            }
            if (end.HasValue)
            {
                query = query.Where(p => p.NgayHuy <= end.Value);
            }
            if (!string.IsNullOrWhiteSpace(maNV))
            {
                query = query.Where(p => p.MaNV == maNV);
            }

            var list = await query.OrderByDescending(p => p.NgayHuy).ToListAsync();

            // map NV name
            var maSet = list.Select(p => p.MaNV).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var nvNames = await _context.NhanViens.AsNoTracking()
                .Where(n => maSet.Contains(n.MaNV))
                .ToDictionaryAsync(n => n.MaNV, n => n.HoTen);

            var outList = new List<dynamic>();
            foreach (var p in list)
            {
                dynamic item = new System.Dynamic.ExpandoObject();
                var d = (IDictionary<string, object>)item;
                d["MaPH"] = p.MaPH;
                d["MaPXH"] = p.MaPXH ?? string.Empty;
                d["NgayHuy"] = p.NgayHuy;
                d["MaNV"] = p.MaNV ?? string.Empty;
                d["NhanVienName"] = (p.MaNV != null && nvNames.ContainsKey(p.MaNV)) ? (nvNames[p.MaNV] ?? string.Empty) : string.Empty;
                d["GhiChu"] = p.GhiChu ?? string.Empty;
                d["TongTien"]=p.TongTien;
                outList.Add(item);
            }

            return outList;
        }

        public async Task<dynamic?> GetDetailsAsync(string maPH)
        {
            if (string.IsNullOrWhiteSpace(maPH)) throw new ArgumentException("MaPH is required");

            var header = await _context.PhieuHuys.AsNoTracking().FirstOrDefaultAsync(p => p.MaPH == maPH);
            if (header == null) return null;

            // Enrich details with MaThuoc and TenThuoc via TonKho -> Thuoc
            var details = await _context.ChiTietPhieuHuys.AsNoTracking()
                .Where(ct => ct.MaPH == maPH)
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
                    x.ct.MaCTPH,
                    x.ct.MaPH,
                    x.ct.MaLo,
                    MaThuoc = x.tk != null ? x.tk.MaThuoc : null,
                    TenThuoc = x.th != null ? x.th.TenThuoc : null,
                    MaLoaiDonVi = x.tk != null ? x.tk.MaLoaiDonViTinh : null,
                    TenLoaiDonVi = ldv != null ? ldv.TenLoaiDonVi : null,
                    SoLuong = (int?)x.ct.SoLuong,
                    x.ct.DonGia,
                    x.ct.ThanhTien,
                    x.ct.GhiChu
                })
                .OrderBy(r => r.MaCTPH)
                .ToListAsync();

            // map NV name
            string? nvName = null;
            if (!string.IsNullOrWhiteSpace(header.MaNV))
            {
                nvName = await _context.NhanViens.AsNoTracking()
                    .Where(n => n.MaNV == header.MaNV)
                    .Select(n => n.HoTen)
                    .FirstOrDefaultAsync();
            }

            dynamic result = new System.Dynamic.ExpandoObject();
            var d = (IDictionary<string, object>)result;
            d["MaPH"] = header.MaPH;
            d["MaPXH"] = header.MaPXH ?? string.Empty;
            d["NgayHuy"] = header.NgayHuy;
            d["MaNV"] = header.MaNV;
            d["NhanVienName"] = nvName ?? string.Empty;
            d["GhiChu"] = header.GhiChu ?? string.Empty;
            // Totals/LyDoHuy not present in DB; omit
            d["ChiTiets"] = details;

            return result;
        }

        // Tạo phiếu huỷ từ MaPXH:
        // - Nếu chi tiết PXH thuộc hoá đơn và chi tiết hoá đơn đã TrangThaiXuLy=true => cập nhật tồn kho giảm theo số lượng chi tiết
        // - Ngược lại: thêm vào ChiTietPhieuHuy
        // - Nếu PXH có MaHD => update HoaDon.TrangThaiGiaoHang = -3
        public async Task<dynamic> CreateFromPXHAsync(BE_QLTiemThuoc.Dto.CreateByPxhDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.MaPXH)) throw new ArgumentException("MaPXH is required");
            if (string.IsNullOrWhiteSpace(dto.MaNV)) throw new ArgumentException("MaNV is required");

            var pxh = await _context.PhieuXuLyHoanHuys.FirstOrDefaultAsync(p => p.MaPXH == dto.MaPXH);
            if (pxh == null) throw new KeyNotFoundException($"PhieuXuLyHoanHuy '{dto.MaPXH}' không tồn tại");

            var ctPxh = await _context.ChiTietPhieuXuLys
                .Where(c => c.MaPXH == dto.MaPXH)
                .AsNoTracking()
                .ToListAsync();
            if (ctPxh.Count == 0) throw new InvalidOperationException("Phiếu xử lý không có chi tiết");

            var maPH = GenerateMaPhieuHuy();
            var ph = new PhieuHuy
            {
                MaPH = maPH,
                MaPXH = dto.MaPXH,
                NgayHuy = DateTime.Now,
                MaNV = dto.MaNV,
                GhiChu = dto.GhiChu
            };

            var createdDetails = new List<object>();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.PhieuHuys.AddAsync(ph);
                await _context.SaveChangesAsync();

                foreach (var ct in ctPxh)
                {
                    var qty = ct.SoLuong ?? 0;
                    var isReturnToStock = ct.LoaiXuLy == true; // 1 = Nhập kho lại, 0 = Huỷ luôn

                    if (isReturnToStock)
                    {
                        // Nhập kho lại: tăng tồn kho theo lô
                        var tk = await _context.TonKhos.FromSqlRaw("SELECT * FROM TON_KHO WITH (UPDLOCK, ROWLOCK) WHERE MaLo = {0}", ct.MaLo)
                            .AsTracking().FirstOrDefaultAsync();
                        if (tk == null) throw new KeyNotFoundException($"TonKho '{ct.MaLo}' không tồn tại");
                        tk.SoLuongCon += qty;
                        _context.TonKhos.Update(tk);
                    }
                    else
                    {
                        // Huỷ luôn: không thao tác tồn kho (đã trừ trước đó), chỉ ghi chi tiết phiếu huỷ
                        var maCTPH = GenerateMaChiTietPhieuHuy();
                        var tkCurrent = await _context.TonKhos.AsNoTracking().FirstOrDefaultAsync(x => x.MaLo == ct.MaLo);
                        var donGia = await GetDonGiaByMaLoAsync(ct.MaLo ?? string.Empty, tkCurrent?.MaThuoc ?? string.Empty, tkCurrent?.MaLoaiDonViTinh ?? string.Empty);
                        var ctPh = new ChiTietPhieuHuy
                        {
                            MaCTPH = maCTPH,
                            MaPH = maPH,
                            MaLo = ct.MaLo ?? string.Empty,
                            MaThuoc = tkCurrent?.MaThuoc ?? string.Empty,
                            MaLoaiDonVi = tkCurrent?.MaLoaiDonViTinh ?? string.Empty,
                            SoLuong = qty,
                            DonGia = donGia,
                            ThanhTien = (donGia > 0 && qty > 0) ? donGia * qty : 0,
                            GhiChu = "Hủy theo PXH"
                        };
                        await _context.ChiTietPhieuHuys.AddAsync(ctPh);
                        createdDetails.Add(new { ctPh.MaCTPH, ctPh.MaLo, SoLuong = ctPh.SoLuong });
                    }
                }

                // Nếu có mã hoá đơn, cập nhật trạng thái -3
                if (!string.IsNullOrWhiteSpace(pxh.MaHD))
                {
                    var hd = await _context.HoaDons.FirstOrDefaultAsync(h => h.MaHD == pxh.MaHD);
                    if (hd != null)
                    {
                        hd.TrangThaiGiaoHang = -3;
                        _context.HoaDons.Update(hd);
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return new { MaPH = maPH, CreatedDetails = createdDetails };
            }
            catch (Exception)
            {
                try { await tx.RollbackAsync(); } catch { /* ignore rollback errors to preserve original */ }
                throw;
            }
        }
    }
}