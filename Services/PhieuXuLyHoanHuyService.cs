using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Data;
using System;
using System.Linq;

namespace BE_QLTiemThuoc.Services
{
    public class PhieuXuLyHoanHuyService
    {
        private readonly PhieuXuLyHoanHuyRepository _repo;
        private readonly AppDbContext _ctx;

        public PhieuXuLyHoanHuyService(PhieuXuLyHoanHuyRepository repo, AppDbContext ctx)
        {
            _repo = repo;
            _ctx = ctx;
        }

        public async Task<List<dynamic>> GetAllAsync(System.DateTime? start = null, System.DateTime? end = null, string? maNV_Tao = null, bool? loaiNguon = null, int? trangThai = null)
        {
            var list = await _repo.GetAllAsync(start, end, maNV_Tao, loaiNguon, trangThai);
            var outList = new List<dynamic>();

            // collect maNV codes to fetch names (cast to string to avoid dynamic LINQ overload issues)
            var maNvs = list.Select(p => (string?)p.MaNV_Tao).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var maNvsDuyet = list.Select(p => (string?)p.MaNV_Duyet).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var allMa = maNvs.Concat(maNvsDuyet).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var nvMap = new Dictionary<string, string?>();
            if (allMa.Any())
            {
                nvMap = _ctx.NhanViens.AsNoTracking()
                    .Where(n => allMa.Contains(n.MaNV))
                    .ToDictionary(n => n.MaNV, n => n.HoTen);
            }

            foreach (var p in list)
            {
                dynamic item = new System.Dynamic.ExpandoObject();
                var d = (IDictionary<string, object>)item;
                d["MaPXH"] = p.MaPXH;
                d["NgayTao"] = p.NgayTao;
                d["MaNV_Tao"] = p.MaNV_Tao;
                d["MaNV_Duyet"] = p.MaNV_Duyet;
                d["NhanVienTaoName"] = (p.MaNV_Tao != null && nvMap.ContainsKey(p.MaNV_Tao)) ? nvMap[p.MaNV_Tao] : null;
                d["NhanVienDuyetName"] = (p.MaNV_Duyet != null && nvMap.ContainsKey(p.MaNV_Duyet)) ? nvMap[p.MaNV_Duyet] : null;
                d["MaHD"] = p.MaHD;
                d["LoaiNguon"] = p.LoaiNguon;
                d["TrangThai"] = p.TrangThai;
                d["LyDo"] = p.GhiChu;
                outList.Add(item);
            }

            return outList;
        }

        public async Task<dynamic?> GetDetailsAsync(string maPXH)
        {
            return await _repo.GetDetailsByMaAsync(maPXH);
        }

        // Return requests filtered by optional date range and status.
        // If both start and end are null, returns all matching status (or all if status null).
        public async Task<List<dynamic>> GetRequestsAsync(DateTime? start, DateTime? end, int? status)
        {
            return await GetAllAsync(start, end, null, null, status);
        }

        // List approved PXH (TrangThai = 1) within date range, with cancel status joined from PhieuHuy
        public async Task<List<dynamic>> GetApprovedWithCancelStatusAsync(DateTime? start, DateTime? end, string? maNV_Tao = null, bool? loaiNguon = null)
        {
            var query = _ctx.PhieuXuLyHoanHuys.AsNoTracking().Where(p => p.TrangThai == 1);
            if (start.HasValue) query = query.Where(p => p.NgayTao >= start.Value);
            if (end.HasValue) query = query.Where(p => p.NgayTao <= end.Value);
            if (!string.IsNullOrWhiteSpace(maNV_Tao)) query = query.Where(p => p.MaNV_Tao == maNV_Tao);
            if (loaiNguon.HasValue) query = query.Where(p => p.LoaiNguon == loaiNguon);

            var list = await query.OrderByDescending(p => p.NgayTao).ToListAsync();

            // Prefetch cancel map by MaPXH
            var keys = list.Select(p => p.MaPXH).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
            var cancelMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keys.Count > 0)
            {
                var cancels = await _ctx.PhieuHuys.AsNoTracking().Where(ph => ph.MaPXH != null && keys.Contains(ph.MaPXH)).Select(ph => ph.MaPXH!).ToListAsync();
                cancelMap = new HashSet<string>(cancels, StringComparer.OrdinalIgnoreCase);
            }

            var outList = new List<dynamic>();
            foreach (var p in list)
            {
                dynamic item = new System.Dynamic.ExpandoObject();
                var d = (IDictionary<string, object>)item;
                d["MaPXH"] = p.MaPXH;
                d["NgayTao"] = p.NgayTao ?? DateTime.MinValue;
                d["MaNV_Tao"] = p.MaNV_Tao ?? string.Empty;
                d["TrangThai"] = p.TrangThai ?? 0;
                d["MaHD"] = p.MaHD ?? string.Empty;
                d["LoaiNguon"] = p.LoaiNguon ?? false;
                d["trangthaitaophieuhuy"] = (!string.IsNullOrWhiteSpace(p.MaPXH) && cancelMap.Contains(p.MaPXH)) ? 1 : 0;
                outList.Add(item);
            }

            return outList;
        }

        // Approve a PXH: set TrangThai = 1 (true/approved) and MaNV_Duyet
        public async Task<bool> ApproveAsync(string maPXH, string maNV_Duyet)
        {
            if (string.IsNullOrWhiteSpace(maPXH)) throw new ArgumentException("maPXH is required");
            if (string.IsNullOrWhiteSpace(maNV_Duyet)) throw new ArgumentException("maNV_Duyet is required");

            var pxh = await _ctx.PhieuXuLyHoanHuys.FirstOrDefaultAsync(p => p.MaPXH == maPXH);
            if (pxh == null) throw new KeyNotFoundException($"PhieuXuLyHoanHuy '{maPXH}' không tồn tại.");

            pxh.TrangThai = 1; // approved
            pxh.MaNV_Duyet = maNV_Duyet;
            _ctx.PhieuXuLyHoanHuys.Update(pxh);
            await _ctx.SaveChangesAsync();
            return true;
        }

        // Update PXH header fields: always set TrangThai = 1 (true), plus GhiChu/LyDo, MaNV_Duyet
        public async Task<bool> UpdateHeaderAsync(string maPXH, string? ghiChu, string? maNV_Duyet)
        {
            if (string.IsNullOrWhiteSpace(maPXH)) throw new ArgumentException("maPXH is required");
            var pxh = await _ctx.PhieuXuLyHoanHuys.FirstOrDefaultAsync(p => p.MaPXH == maPXH);
            if (pxh == null) throw new KeyNotFoundException($"PhieuXuLyHoanHuy '{maPXH}' không tồn tại.");

            // Always set approved/true regardless of input
            pxh.TrangThai = 1;
            if (!string.IsNullOrWhiteSpace(maNV_Duyet)) pxh.MaNV_Duyet = maNV_Duyet;

            // support either LyDo or GhiChu property on model
            void SetLyDoIfPossible(object target, string? value)
            {
                var pi = target.GetType().GetProperty("LyDo") ?? target.GetType().GetProperty("GhiChu");
                if (pi != null && pi.CanWrite) pi.SetValue(target, value);
            }
            if (ghiChu != null) SetLyDoIfPossible(pxh, ghiChu);

            _ctx.PhieuXuLyHoanHuys.Update(pxh);
            await _ctx.SaveChangesAsync();
            return true;
        }

        // Update PXH and optional its details via DTO
        public async Task<bool> UpdateWithDetailsAsync(Dto.UpdatePhieuXuLyHoanHuyDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.MaPXH)) throw new ArgumentException("MaPXH is required");

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
            // Force status to true on update
            await UpdateHeaderAsync(dto.MaPXH!, dto.GhiChu, dto.MaNV_Duyet);

                if (dto.ChiTiets != null && dto.ChiTiets.Count > 0)
                {
                    foreach (var ct in dto.ChiTiets)
                    {
                        if (ct == null || string.IsNullOrWhiteSpace(ct.MaCTPXH)) continue;
                        var row = await _ctx.Set<ChiTietPhieuXuLy>().FirstOrDefaultAsync(x => x.MaCTPXH == ct.MaCTPXH && x.MaPXH == dto.MaPXH);
                        if (row == null) continue;
                        if (ct.SoLuong.HasValue)
                        {
                            // do not allow negative values
                            var val = ct.SoLuong.Value;
                            if (val < 0) throw new ArgumentException("SoLuong chi tiết không hợp lệ");
                            row.SoLuong = val;
                        }
                        if (ct.LoaiXuLy.HasValue)
                        {
                            row.LoaiXuLy = ct.LoaiXuLy.Value;
                        }
                        _ctx.Set<ChiTietPhieuXuLy>().Update(row);
                    }
                }

                await _ctx.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<dynamic> CreateQuickRequestAsync(Dto.PhieuXuLyHoanHuyQuickCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.MaNV)) throw new ArgumentException("MaNV is required");
            if (string.IsNullOrWhiteSpace(dto.MaThuoc)) throw new ArgumentException("MaThuoc is required");
            if (string.IsNullOrWhiteSpace(dto.MaLoaiDonVi)) throw new ArgumentException("MaLoaiDonVi is required");
            if (dto.SoLuong <= 0) throw new ArgumentException("SoLuong must be > 0");

            // find candidate lots ordered by nearest expiry (earliest HanSuDung first)
            var candidateLots = await _ctx.TonKhos
                .Where(t => t.MaThuoc == dto.MaThuoc && t.MaLoaiDonViTinh == dto.MaLoaiDonVi && t.SoLuongCon > 0)
                .OrderBy(t => t.HanSuDung)
                .ToListAsync();

            var totalAvailable = candidateLots.Sum(t => t.SoLuongCon);
            if (totalAvailable < dto.SoLuong) throw new InvalidOperationException("Insufficient total stock to satisfy the request");

            // create PXH header
            var maPXH = "PXH" + DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString();
            var phieu = new PhieuXuLyHoanHuy
            {
                MaPXH = maPXH,
                NgayTao = DateTime.Now,
                MaNV_Tao = dto.MaNV,
                LoaiNguon = false, // from kho
                TrangThai = 0, // chờ duyệt
                MaHD = null,
            };

            // Set LyDo or fallback GhiChu via reflection if the model has that property
            void SetLyDoIfPossible(object target, string? value)
            {
                var pi = target.GetType().GetProperty("LyDo");
                if (pi == null)
                {
                    pi = target.GetType().GetProperty("GhiChu");
                }
                if (pi != null && pi.CanWrite)
                {
                    pi.SetValue(target, value);
                }
            }

            var remaining = dto.SoLuong;
            var createdCTs = new List<dynamic>();

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                await _ctx.PhieuXuLyHoanHuys.AddAsync(phieu);
                // assign LyDo/GhiChu on the tracked entity
                SetLyDoIfPossible(phieu, dto.LyDo);
                // Save header first so the FK constraint for ChiTietPhieuXuLy is satisfied
                await _ctx.SaveChangesAsync();

                foreach (var lotItem in candidateLots)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(remaining, lotItem.SoLuongCon);
                    if (take <= 0) continue;

                    var maCT = "CTPXH" + DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString();
                    var ct = new ChiTietPhieuXuLy
                    {
                        MaCTPXH = maCT,
                        MaPXH = maPXH,
                        MaLo = lotItem.MaLo,
                        SoLuong = take,
                        LoaiXuLy = false
                    };

                    await _ctx.Set<ChiTietPhieuXuLy>().AddAsync(ct);

                    lotItem.SoLuongCon -= take;
                    if (lotItem.SoLuongCon < 0) throw new InvalidOperationException("Inventory went negative unexpectedly");
                    _ctx.TonKhos.Update(lotItem);

                    createdCTs.Add(new { MaCT = maCT, MaLo = lotItem.MaLo, SoLuong = take, SoLuongConRemaining = lotItem.SoLuongCon });

                    remaining -= take;
                }

                if (remaining > 0) throw new InvalidOperationException("Unable to allocate required quantity from lots");

                await _ctx.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            dynamic outObj = new System.Dynamic.ExpandoObject();
            var d = (IDictionary<string, object>)outObj;
            d["MaPXH"] = maPXH;
            d["CTs"] = createdCTs;
            return outObj;
        }

        /// <summary>
        /// Create a PhieuXuLyHoanHuy with an explicit list of detail items.
        /// Each detail must reference an existing MaLo and SoLuong to consume from that lot.
        /// All updates are performed transactionally: header saved first, then CT rows and TonKho updates.
        /// </summary>
        public async Task<dynamic> CreateFromDetailsAsync(Dto.PhieuXuLyHoanHuyCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.MaNV)) throw new ArgumentException("MaNV is required");
            if (dto.ChiTiets == null || !dto.ChiTiets.Any()) throw new ArgumentException("ChiTiets is required and must contain at least one item");

            var maPXH = "PXH" + DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString();
            var phieu = new PhieuXuLyHoanHuy
            {
                MaPXH = maPXH,
                NgayTao = DateTime.Now,
                MaNV_Tao = dto.MaNV,
                LoaiNguon = dto.LoaiNguon ?? false,
                TrangThai = 0,
                MaHD = dto.MaHD
            };

            void SetLyDoIfPossible(object target, string? value)
            {
                var pi = target.GetType().GetProperty("LyDo");
                if (pi == null)
                {
                    pi = target.GetType().GetProperty("GhiChu");
                }
                if (pi != null && pi.CanWrite)
                {
                    pi.SetValue(target, value);
                }
            }

            var createdCTs = new List<dynamic>();

            await using var tx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                await _ctx.PhieuXuLyHoanHuys.AddAsync(phieu);
                SetLyDoIfPossible(phieu, dto.GhiChu);
                await _ctx.SaveChangesAsync();

                foreach (var item in dto.ChiTiets)
                {
                    if (item == null) throw new ArgumentException("ChiTiet item cannot be null");
                    if (string.IsNullOrWhiteSpace(item.MaLo)) throw new ArgumentException("MaLo is required for each ChiTiet");
                    if (item.SoLuong <= 0) throw new ArgumentException("SoLuong must be > 0 for each ChiTiet");

                    // lock the lot row before mutating
                    var tk = await _ctx.TonKhos.FromSqlRaw("SELECT * FROM TON_KHO WITH (UPDLOCK, ROWLOCK) WHERE MaLo = {0}", item.MaLo).AsTracking().FirstOrDefaultAsync();
                    if (tk == null) throw new KeyNotFoundException($"TonKho '{item.MaLo}' not found");

                    // If the source is from kho (LoaiNguon == false) then we must check and decrement inventory.
                    if (!(dto.LoaiNguon ?? false))
                    {
                        if (tk.SoLuongCon < item.SoLuong) throw new InvalidOperationException($"Not enough quantity in lot {item.MaLo} to consume {item.SoLuong}");

                        tk.SoLuongCon -= item.SoLuong;
                        _ctx.TonKhos.Update(tk);
                    }

                    var maCT = "CTPXH" + DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString();
                    var ct = new ChiTietPhieuXuLy
                    {
                        MaCTPXH = maCT,
                        MaPXH = maPXH,
                        MaLo = item.MaLo,
                        SoLuong = item.SoLuong,
                        LoaiXuLy = item.LoaiXuLy ?? false
                    };

                    await _ctx.Set<ChiTietPhieuXuLy>().AddAsync(ct);

                    createdCTs.Add(new { MaCT = maCT, MaLo = item.MaLo, SoLuong = item.SoLuong, SoLuongConRemaining = tk.SoLuongCon });

                    // If source is invoice (LoaiNguon == true) and MaHD provided,
                    // mark corresponding invoice detail rows as processed (TrangThaiXuLy = true)
                    if ((dto.LoaiNguon ?? false) && !string.IsNullOrWhiteSpace(dto.MaHD))
                    {
                        var relatedDetails = await _ctx.ChiTietHoaDons
                            .Where(x => x.MaHD == dto.MaHD && x.MaLo == item.MaLo)
                            .ToListAsync();
                        if (relatedDetails.Any())
                        {
                            foreach (var r in relatedDetails)
                            {
                                r.TrangThaiXuLy = true;
                            }
                            _ctx.ChiTietHoaDons.UpdateRange(relatedDetails);
                        }
                    }
                }

                // If source is invoice (LoaiNguon == true) and MaHD provided,
                // update HoaDon.TrangThaiGiaoHang: -3 if all details processed, otherwise -2
                if ((dto.LoaiNguon ?? false) && !string.IsNullOrWhiteSpace(dto.MaHD))
                {
                    var hd = await _ctx.HoaDons.FirstOrDefaultAsync(h => h.MaHD == dto.MaHD);
                    if (hd != null)
                    {
                        var details = await _ctx.ChiTietHoaDons.Where(ct => ct.MaHD == dto.MaHD).ToListAsync();
                        if (details.Count > 0)
                        {
                            bool allProcessed = details.All(ct => ct.TrangThaiXuLy == true);
                            hd.TrangThaiGiaoHang = allProcessed ? -3 : -2;
                            _ctx.HoaDons.Update(hd);
                        }
                    }
                }

                await _ctx.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            dynamic outObj = new System.Dynamic.ExpandoObject();
            var d = (IDictionary<string, object>)outObj;
            d["MaPXH"] = maPXH;
            d["CTs"] = createdCTs;
            return outObj;
        }
    }
}