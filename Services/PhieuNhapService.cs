using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Services
{
    public class PhieuNhapService
    {
        private readonly PhieuNhapRepository _repo;
        private readonly AppDbContext _context;

        public PhieuNhapService(PhieuNhapRepository repo, AppDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public async Task<List<object>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, string? maNV = null, string? maNCC = null)
        {
            var ctx = _repo.Context;
            var query = ctx.PhieuNhaps.AsQueryable()
                .Where(pn => pn.NgayNhap >= startDate && pn.NgayNhap <= endDate);

            if (!string.IsNullOrWhiteSpace(maNV))
            {
                query = query.Where(pn => pn.MaNV == maNV);
            }
            if (!string.IsNullOrWhiteSpace(maNCC))
            {
                query = query.Where(pn => pn.MaNCC == maNCC);
            }

            var phieuNhaps = await query
                .Select(pn => new
                {
                    pn.MaPN,
                    pn.NgayNhap,
                    pn.TongTien,
                    pn.GhiChu,
                    pn.MaNCC,
                    pn.MaNV,
                    TenNCC = ctx.NhaCungCaps.Where(ncc => ncc.MaNCC == pn.MaNCC).Select(ncc => ncc.TenNCC).FirstOrDefault(),
                    TenNV = ctx.Set<NhanVien>().Where(nv => nv.MaNV == pn.MaNV).Select(nv => nv.HoTen).FirstOrDefault()
                })
                .ToListAsync();

            return phieuNhaps.Cast<object>().ToList();
        }

        public async Task<object> AddPhieuNhapAsync(PhieuNhapDto phieuNhapDto)
        {
            if (phieuNhapDto == null || phieuNhapDto.ChiTietPhieuNhaps == null) throw new ArgumentException("Invalid input data");

            // Start transaction via repository
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var entryDate = (phieuNhapDto.NgayNhap != default(DateTime)) ? phieuNhapDto.NgayNhap : DateTime.Now;
                string? maPN = phieuNhapDto.MaPN;
                if (string.IsNullOrEmpty(maPN))
                {
                    var year = entryDate.Year;
                    var month = entryDate.Month;
                    var countInMonth = await _repo.CountPhieuNhapsInMonthAsync(year, month);
                    var nextSeq = countInMonth + 1;
                    var yy = year % 100;
                    maPN = $"PN{nextSeq.ToString("D4")}/{yy:D2}-{month:D2}";
                }

                var phieuNhap = new PhieuNhap
                {
                    MaPN = maPN,
                    NgayNhap = entryDate,
                    MaNCC = phieuNhapDto.MaNCC,
                    MaNV = phieuNhapDto.MaNV,
                    TongTien = phieuNhapDto.TongTien,
                    GhiChu = phieuNhapDto.GhiChu
                };

                await _repo.AddPhieuNhapAsync(phieuNhap);

                var existingCount = await _repo.CountChiTietByMaPNAsync(phieuNhap.MaPN!);
                int idx = 0;
                string maPNSuffix = "000";
                var chiTietEntities = new List<ChiTietPhieuNhap>();
                // Assign MaCTPN for each incoming DTO (we will create entities after lot assignments)

                try
                {
                    var pn = phieuNhap.MaPN ?? string.Empty;
                    var parts = pn.Split('/');
                    if (parts.Length > 0)
                    {
                        var left = parts[0];
                        if (left.StartsWith("PN")) left = left.Substring(2);
                        if (!string.IsNullOrEmpty(left))
                        {
                            var digits = left.Length <= 3 ? left : left.Substring(left.Length - 3);
                            maPNSuffix = digits.PadLeft(3, '0');
                        }
                    }
                }
                catch { maPNSuffix = "000"; }

                foreach (var dto in phieuNhapDto.ChiTietPhieuNhaps)
                {
                    idx++;
                    string? maCTPN = dto.MaCTPN;
                    if (string.IsNullOrEmpty(maCTPN))
                    {
                        var seq = existingCount + idx;
                        var yy = phieuNhap.NgayNhap.Year % 100;
                        var mm = phieuNhap.NgayNhap.Month;
                        maCTPN = $"CTPN{seq.ToString("D3")}/{maPNSuffix}/{yy:D2}-{mm:D2}";
                    }
                    dto.MaCTPN = maCTPN;
                }

                // Units mapping
                var unitCodes = phieuNhapDto.ChiTietPhieuNhaps
                    .Select(x => x.MaLoaiDonViNhap)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.Trim())
                    .Distinct()
                    .ToList();

                var unitNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var unitRows = new List<PhieuNhapRepository.UnitRow>();
                if (unitCodes.Any())
                {
                    unitRows = (await _repo.GetLoaiDonViByCodesAsync(unitCodes)).ToList();
                    foreach (var u in unitRows)
                    {
                        if (!string.IsNullOrEmpty(u.MaLoaiDonVi))
                            unitNameByCode[u.MaLoaiDonVi] = u.TenLoaiDonVi ?? u.MaLoaiDonVi;
                    }

                    // Validate that provided unit codes actually exist in LoaiDonVi table
                    var foundCodes = new HashSet<string>(unitRows.Where(x => !string.IsNullOrEmpty(x.MaLoaiDonVi)).Select(x => x.MaLoaiDonVi!), StringComparer.OrdinalIgnoreCase);
                    var missingUnitCodes = unitCodes.Where(c => !foundCodes.Contains(c)).ToList();
                    if (missingUnitCodes.Any())
                    {
                        throw new InvalidOperationException("Missing LoaiDonVi codes: " + string.Join(",", missingUnitCodes));
                    }
                }

                // Validate that each MaLoaiDonViNhap exists in GIATHUOC for the corresponding MaThuoc
                var requiredPairs = phieuNhapDto.ChiTietPhieuNhaps
                    .Where(d => !string.IsNullOrWhiteSpace(d.MaLoaiDonViNhap) && !string.IsNullOrWhiteSpace(d.MaThuoc))
                    .Select(d => new { MaThuoc = d.MaThuoc!.Trim(), MaLoai = d.MaLoaiDonViNhap!.Trim() })
                    .Distinct()
                    .ToList();

                if (requiredPairs.Any())
                {
                    var maThuocList = requiredPairs.Select(r => r.MaThuoc).Distinct().ToList();
                    var maLoaiList = requiredPairs.Select(r => r.MaLoai).Distinct().ToList();

                    var existingPairs = await _repo.Context.Set<GiaThuoc>()
                        .Where(gt => maThuocList.Contains(gt.MaThuoc) && maLoaiList.Contains(gt.MaLoaiDonVi))
                        .Select(gt => new { gt.MaThuoc, gt.MaLoaiDonVi })
                        .ToListAsync();

                    var existingSet = new HashSet<(string, string)>(existingPairs.Select(x => (x.MaThuoc ?? string.Empty, x.MaLoaiDonVi ?? string.Empty)));

                    var missing = requiredPairs.Where(r => !existingSet.Contains((r.MaThuoc, r.MaLoai))).ToList();
                    if (missing.Any())
                    {
                        var messages = missing.Select(m => $"MaLoaiDonVi '{m.MaLoai}' không tồn tại cho MaThuoc '{m.MaThuoc}'");
                        throw new InvalidOperationException("Validation failed: " + string.Join("; ", messages));
                    }
                }

                // Build key sets
                var keyPairs = phieuNhapDto.ChiTietPhieuNhaps
                    .Select(d => new { MaThuoc = d.MaThuoc ?? string.Empty, HSD = d.HanSuDung ?? phieuNhap.NgayNhap })
                    .ToList();
                var maThuocSet = keyPairs.Select(k => k.MaThuoc).Distinct().ToList();
                var hsdSet = keyPairs.Select(k => k.HSD.Date).Distinct().ToList();

                var existingLots = await _repo.GetExistingLotsAsync(maThuocSet, hsdSet);
                var lotLookup = new Dictionary<(string, DateTime), TonKho>();
                foreach (var lot in existingLots)
                {
                    var key = (lot.MaThuoc, lot.HanSuDung.Date);
                    if (!lotLookup.ContainsKey(key)) lotLookup[key] = lot;
                }

                int lotIndex = 0;
                foreach (var dto in phieuNhapDto.ChiTietPhieuNhaps)
                {
                    var maThuoc = dto.MaThuoc ?? string.Empty;
                    var hsd = (dto.HanSuDung ?? phieuNhap.NgayNhap).Date;
                    var key = (maThuoc, hsd);

                    if (lotLookup.TryGetValue(key, out var existing))
                    {
                        existing.SoLuongNhap += dto.SoLuong;
                        existing.SoLuongCon += dto.SoLuong;
                    }
                    else
                    {
                        lotIndex++;
                        var maLo = GenMaLo(lotIndex);
                        var code = (dto.MaLoaiDonViNhap ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(code)) throw new InvalidOperationException($"MaLoaiDonViNhap is required for MaThuoc '{maThuoc}'");
                        // Use the unit code for the FK column (TonKho.MaLoaiDonViTinh) and ensure length fits
                        var donViTinhCode = code;
                        if (donViTinhCode.Length > 10) donViTinhCode = donViTinhCode.Substring(0, 10);
                        var ghi = phieuNhapDto.GhiChu ?? string.Empty;
                        if (ghi.Length > 255) ghi = ghi.Substring(0, 255);

                        var newLot = new TonKho
                        {
                            MaLo = maLo,
                            MaThuoc = maThuoc,
                            HanSuDung = hsd,
                            TrangThaiSeal = false,
                            MaLoaiDonViTinh = donViTinhCode,
                            SoLuongNhap = dto.SoLuong,
                            SoLuongCon = dto.SoLuong,
                            GhiChu = ghi
                        };
                        await _repo.Context.TonKhos.AddAsync(newLot);
                        lotLookup[key] = newLot;
                    }
                }

                await _repo.SaveChangesAsync();

                // Now create ChiTietPhieuNhap entities, setting MaLo and MaLoaiDonVi per new schema
                foreach (var dto in phieuNhapDto.ChiTietPhieuNhaps)
                {
                    var maThuoc = dto.MaThuoc ?? string.Empty;
                    var hsd = (dto.HanSuDung ?? phieuNhap.NgayNhap).Date;
                    var key = (maThuoc, hsd);
                    string? maLo = null;
                    if (lotLookup.TryGetValue(key, out var lot)) maLo = lot.MaLo;

                    var ent = new ChiTietPhieuNhap
                    {
                        MaCTPN = dto.MaCTPN ?? string.Empty,
                        MaPN = phieuNhap.MaPN!,
                        MaThuoc = maThuoc,
                        MaLo = maLo,
                        MaLoaiDonVi = dto.MaLoaiDonViNhap,
                        SoLuong = dto.SoLuong,
                        DonGia = dto.DonGia,
                        ThanhTien = dto.ThanhTien,
                        HanSuDung = dto.HanSuDung ?? phieuNhap.NgayNhap,
                        GhiChu = dto.GhiChu
                    };
                    chiTietEntities.Add(ent);
                }

                if (chiTietEntities.Any()) await _repo.AddChiTietRangeAsync(chiTietEntities);

                await _repo.SaveChangesAsync();

                await tx.CommitAsync();

                // collect MaLo values used/created for this PhieuNhap
                var maLos = lotLookup.Values.Select(l => l.MaLo).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();

                return new { MaPN = phieuNhap.MaPN ?? string.Empty, MaLos = maLos };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static string GenMaLo(int index)
        {
            var ts = DateTime.UtcNow.ToString("yyMMddHHmmss");
            var id = $"LO{ts}{index:D2}";
            return id;
        }

        public async Task<object> GetChiTietPhieuNhapByMaPNAsync(string maPN)
        {
            // This method will be called by the controller; keep the same projection as before.
            // For simplicity reuse repository's context via internal access (not ideal but keeps changes minimal).
            var ctx = _repo.Context;
            var phieuNhap = await ctx.PhieuNhaps
                .Where(pn => pn.MaPN == maPN)
                .Select(pn => new
                {
                    pn.MaPN,
                    pn.NgayNhap,
                    pn.TongTien,
                    pn.GhiChu,
                    pn.MaNCC,
                    TenNCC = ctx.NhaCungCaps.Where(ncc => ncc.MaNCC == pn.MaNCC).Select(ncc => ncc.TenNCC).FirstOrDefault(),
                    TenNV = ctx.Set<NhanVien>().Where(nv => nv.MaNV == pn.MaNV).Select(nv => nv.HoTen).FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            var chiTietPhieuNhaps = await ctx.Set<ChiTietPhieuNhap>()
                .Where(ctpn => ctpn.MaPN == maPN)
                .Select(ctpn => new
                {
                    ctpn.MaCTPN,
                    ctpn.MaPN,
                    ctpn.MaThuoc,
                    TenThuoc = ctx.Thuoc.Where(t => t.MaThuoc == ctpn.MaThuoc).Select(t => t.TenThuoc).FirstOrDefault(),
                    ctpn.SoLuong,
                    ctpn.DonGia,
                    ctpn.ThanhTien,
                    HanSuDung = ctpn.HanSuDung,
                    ctpn.GhiChu,
                    MaLo = ctpn.MaLo,
                    MaLoaiDonVi = ctpn.MaLoaiDonVi,
                    TenLoaiDonVi = ctx.Set<Model.Thuoc.LoaiDonVi>().Where(ld => ld.MaLoaiDonVi == ctpn.MaLoaiDonVi).Select(ld => ld.TenLoaiDonVi).FirstOrDefault()
                })
                .ToListAsync();

            return new { PhieuNhap = phieuNhap, ChiTiet = chiTietPhieuNhaps };
        }

        public async Task<object?> UpdatePhieuNhapAsync(string maPN, PhieuNhapDto dto)
        {
            var existing = await _repo.GetByIdAsync(maPN);
            if (existing == null) return null;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy danh sách chi tiết cũ
                var currentChiTiet = await _repo.Context.Set<ChiTietPhieuNhap>()
                    .Where(c => c.MaPN == maPN)
                    .ToListAsync();

                // Lấy danh sách MaCTPN mới truyền lên
                var inputMaCTPN = dto.ChiTietPhieuNhaps
                    .Where(x => !string.IsNullOrEmpty(x.MaCTPN))
                    .Select(x => x.MaCTPN)
                    .ToHashSet();

                // Xử lý các chi tiết bị xóa (không còn trong input)
                var chiTietToDelete = currentChiTiet
                    .Where(ct => !inputMaCTPN.Contains(ct.MaCTPN))
                    .ToList();

                foreach (var ct in chiTietToDelete)
                {
                    if (!string.IsNullOrEmpty(ct.MaLo))
                    {
                        var tonKho = await _repo.Context.TonKhos.FirstOrDefaultAsync(t => t.MaLo == ct.MaLo);
                        if (tonKho != null)
                        {
                            // Nếu lô này chưa ai sử dụng (SoLuongNhap == SoLuongCon == ct.SoLuong)
                            if (tonKho.SoLuongNhap == tonKho.SoLuongCon && tonKho.SoLuongNhap == ct.SoLuong)
                            {
                                _repo.Context.TonKhos.Remove(tonKho);
                            }
                            else
                            {
                                // Giảm số lượng nhập/xuống như bình thường
                                tonKho.SoLuongNhap -= ct.SoLuong;
                                tonKho.SoLuongCon -= ct.SoLuong;
                            }
                        }
                    }
                    _repo.Context.Set<ChiTietPhieuNhap>().Remove(ct);
                }

                // Cập nhật thông tin phiếu nhập
                existing.GhiChu = dto.GhiChu;
                existing.MaNCC = dto.MaNCC;
                existing.MaNV = dto.MaNV;

                // Xử lý các chi tiết còn lại và thêm mới
                var currentMaCTPN = currentChiTiet.Select(x => x.MaCTPN).ToHashSet();
                int idx = 0;
                string maPNSuffix = "000";
                // ... (tính maPNSuffix như cũ)

                foreach (var ctDto in dto.ChiTietPhieuNhaps)
                {
                    // Nếu là chi tiết mới (không có MaCTPN hoặc MaCTPN không tồn tại trong DB)
                    if (string.IsNullOrEmpty(ctDto.MaCTPN) || !currentMaCTPN.Contains(ctDto.MaCTPN))
                    {
                        idx++;
                        // Tạo mã mới
                        var seq = idx;
                        var yy = existing.NgayNhap.Year % 100;
                        var mm = existing.NgayNhap.Month;
                        var maCTPN = $"CTPN{seq.ToString("D3")}/{maPNSuffix}/{yy:D2}-{mm:D2}";
                        ctDto.MaCTPN = maCTPN;

                        // Xử lý lô như thêm mới (tìm hoặc tạo mới TonKho)
                        // ... (giống logic thêm mới ở AddPhieuNhapAsync)
                        // Sau đó tạo mới ChiTietPhieuNhap
                        // ...
                    }
                    else
                    {
                        // Nếu là chi tiết cũ, update số lượng và các trường khác như bình thường
                        var ctOld = currentChiTiet.FirstOrDefault(x => x.MaCTPN == ctDto.MaCTPN);
                        if (ctOld != null)
                        {
                            // Tìm TonKho và cập nhật số lượng nếu cần
                            if (!string.IsNullOrEmpty(ctOld.MaLo))
                            {
                                var tonKho = await _repo.Context.TonKhos.FirstOrDefaultAsync(t => t.MaLo == ctOld.MaLo);
                                if (tonKho != null)
                                {
                                    // Điều chỉnh số lượng nhập/xuống nếu số lượng thay đổi
                                    int delta = ctDto.SoLuong - ctOld.SoLuong;
                                    tonKho.SoLuongNhap += delta;
                                    tonKho.SoLuongCon += delta;
                                }
                            }
                            // Cập nhật các trường khác của ctOld nếu cần
                            ctOld.SoLuong = ctDto.SoLuong;
                            ctOld.DonGia = ctDto.DonGia;
                            ctOld.ThanhTien = ctDto.ThanhTien;
                            ctOld.HanSuDung = ctDto.HanSuDung ?? existing.NgayNhap;
                            ctOld.GhiChu = ctDto.GhiChu;
                        }
                    }
                }

                await _repo.SaveChangesAsync();
                await tx.CommitAsync();
                return existing;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeletePhieuNhapAsync(string maPN)
        {
            var existing = await _repo.GetByIdAsync(maPN);
            if (existing == null) return false;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get all chi tiet to rollback inventory
                var chiTietList = await _repo.Context.Set<ChiTietPhieuNhap>()
                    .Where(c => c.MaPN == maPN)
                    .ToListAsync();

                // Rollback inventory for each chi tiet
                foreach (var ct in chiTietList)
                {
                    if (!string.IsNullOrEmpty(ct.MaLo))
                    {
                        var tonKho = await _repo.Context.TonKhos.FirstOrDefaultAsync(t => t.MaLo == ct.MaLo);
                        if (tonKho != null)
                        {
                            // Nếu lô này chưa ai sử dụng (SoLuongNhap == SoLuongCon == ct.SoLuong)
                            if (tonKho.SoLuongNhap == tonKho.SoLuongCon && tonKho.SoLuongNhap == ct.SoLuong)
                            {
                                _repo.Context.TonKhos.Remove(tonKho);
                            }
                            else
                            {
                                // Giảm số lượng nhập/xuống như bình thường
                                tonKho.SoLuongNhap -= ct.SoLuong;
                                tonKho.SoLuongCon -= ct.SoLuong;
                            }
                        }
                    }
                }

                // Remove chi tiet records
                _repo.Context.Set<ChiTietPhieuNhap>().RemoveRange(chiTietList);

                // Remove phieu nhap
                _repo.Context.PhieuNhaps.Remove(existing);

                await _repo.SaveChangesAsync();
                await tx.CommitAsync();

                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }

}

