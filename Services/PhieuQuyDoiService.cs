using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Model.Kho;
using BE_QLTiemThuoc.Repositories;
using BE_QLTiemThuoc.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Dynamic;
using System.Collections;

namespace BE_QLTiemThuoc.Services
{
    public class PhieuQuyDoiService
    {
        private readonly PhieuNhapRepository _repo;
        private readonly AppDbContext _context;
        // constructor
        public PhieuQuyDoiService(PhieuNhapRepository repo, AppDbContext context)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Returns a list of PHIEU_QUY_DOI rows (MaPhieuQD, NgayQuyDoi, MaNV, GhiChu) ordered by NgayQuyDoi desc.
        /// </summary>
        public async Task<dynamic> GetAllPhieuQuyDoiAsync(DateTime? start = null, DateTime? end = null, string? maNV = null)
        {
            var result = new List<dynamic>();
            var ctx = _repo.Context;
            // build SQL with optional date filter
            var sql = "SELECT MaPhieuQD, NgayQuyDoi, MaNV, GhiChu FROM PHIEU_QUY_DOI";
            var whereClauses = new List<string>();

            using (var conn = ctx.Database.GetDbConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    if (start.HasValue)
                    {
                        whereClauses.Add("NgayQuyDoi >= @start");
                        var p = cmd.CreateParameter(); p.ParameterName = "@start"; p.Value = start.Value.Date; cmd.Parameters.Add(p);
                    }
                    if (end.HasValue)
                    {
                        whereClauses.Add("NgayQuyDoi <= @end");
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@end"; p2.Value = end.Value.Date.AddDays(1).AddTicks(-1); cmd.Parameters.Add(p2);
                    }

                    if (!string.IsNullOrWhiteSpace(maNV))
                    {
                        whereClauses.Add("MaNV = @maNV");
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@maNV"; p3.Value = maNV; cmd.Parameters.Add(p3);
                    }

                    if (whereClauses.Any()) sql += " WHERE " + string.Join(" AND ", whereClauses);
                    sql += " ORDER BY NgayQuyDoi DESC";

                    cmd.CommandText = sql;
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dynamic item = new ExpandoObject();
                            var d = (IDictionary<string, object>)item;
                            d["MaPhieuQD"] = reader["MaPhieuQD"] == DBNull.Value ? null : reader["MaPhieuQD"].ToString();
                            d["NgayQuyDoi"] = reader["NgayQuyDoi"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["NgayQuyDoi"];
                            d["MaNV"] = reader["MaNV"] == DBNull.Value ? null : reader["MaNV"].ToString();
                            // placeholder for employee name to be filled later
                            d["NhanVienName"] = null;
                            d["GhiChu"] = reader["GhiChu"] == DBNull.Value ? null : reader["GhiChu"].ToString();
                            result.Add(item);
                        }
                    }
                }

                // enrich with employee names
                try
                {
                    var maNvs = result
                        .Select(r => ((IDictionary<string, object>)r).ContainsKey("MaNV") ? ((IDictionary<string, object>)r)["MaNV"] as string : null)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (maNvs.Any())
                    {
                        var nvMap = await ctx.Set<Model.NhanVien>().AsNoTracking()
                            .Where(n => maNvs.Contains(n.MaNV))
                            .ToDictionaryAsync(n => n.MaNV, n => n.HoTen);

                        foreach (var itm in result)
                        {
                            var dict = (IDictionary<string, object>)itm;
                            if (dict.ContainsKey("MaNV") && dict["MaNV"] is string m && !string.IsNullOrWhiteSpace(m))
                            {
                                dict["NhanVienName"] = nvMap.ContainsKey(m) ? nvMap[m] : null;
                            }
                        }
                    }
                }
                catch
                {
                    // non-fatal: if fetching names fails, return list without names
                }
            }

            // wrap into response object including requested date range and optional MaNV filter
            dynamic outObj = new ExpandoObject();
            var od = (IDictionary<string, object>)outObj;
            od["StartDate"] = start;
            od["EndDate"] = end;
            od["MaNV"] = maNV;
            od["Items"] = result;
            return outObj;
        }

        /// <summary>
        /// Returns detail for a phieu quy doi: header + list of CT rows
        /// </summary>
        public async Task<dynamic> GetPhieuQuyDoiDetailsAsync(string maPhieu)
        {
            if (string.IsNullOrWhiteSpace(maPhieu)) throw new ArgumentException("maPhieu is required");

            var ctx = _repo.Context;
            dynamic output = new ExpandoObject();
            var header = new ExpandoObject();
            var headerDict = (IDictionary<string, object>)header;

            var sqlHeader = "SELECT MaPhieuQD, NgayQuyDoi, MaNV, GhiChu FROM PHIEU_QUY_DOI WHERE MaPhieuQD = @maPhieu";
            using (var conn = ctx.Database.GetDbConnection())
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlHeader;
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@maPhieu";
                    p.Value = maPhieu;
                    cmd.Parameters.Add(p);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            headerDict["MaPhieuQD"] = reader["MaPhieuQD"] == DBNull.Value ? null : reader["MaPhieuQD"].ToString();
                            headerDict["NgayQuyDoi"] = reader["NgayQuyDoi"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["NgayQuyDoi"];
                            headerDict["MaNV"] = reader["MaNV"] == DBNull.Value ? null : reader["MaNV"].ToString();
                            headerDict["GhiChu"] = reader["GhiChu"] == DBNull.Value ? null : reader["GhiChu"].ToString();
                        }
                        else
                        {
                            return null; // not found
                        }
                    }
                }

                // now read CT rows
                var ctList = new List<dynamic>();
                using (var cmd2 = conn.CreateCommand())
                {
                    cmd2.CommandText = "SELECT MaPhieuQD, MaLoGoc, MaLoMoi, MaThuoc, SoLuongQuyDoi, TyLeQuyDoi, SoLuongGoc, MaLoaiDonViGoc, MaLoaiDonViMoi FROM CT_PHIEU_QUY_DOI WHERE MaPhieuQD = @maPhieu";
                    var p2 = cmd2.CreateParameter();
                    p2.ParameterName = "@maPhieu";
                    p2.Value = maPhieu;
                    cmd2.Parameters.Add(p2);

                    using (var reader2 = await cmd2.ExecuteReaderAsync())
                    {
                        while (await reader2.ReadAsync())
                        {
                            dynamic ct = new ExpandoObject();
                            var ctD = (IDictionary<string, object>)ct;
                            ctD["MaPhieuQD"] = reader2["MaPhieuQD"] == DBNull.Value ? null : reader2["MaPhieuQD"].ToString();
                            ctD["MaLoGoc"] = reader2["MaLoGoc"] == DBNull.Value ? null : reader2["MaLoGoc"].ToString();
                            ctD["MaLoMoi"] = reader2["MaLoMoi"] == DBNull.Value ? null : reader2["MaLoMoi"].ToString();
                            ctD["MaThuoc"] = reader2["MaThuoc"] == DBNull.Value ? null : reader2["MaThuoc"].ToString();
                            ctD["SoLuongQuyDoi"] = reader2["SoLuongQuyDoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader2["SoLuongQuyDoi"]);
                            ctD["TyLeQuyDoi"] = reader2["TyLeQuyDoi"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader2["TyLeQuyDoi"]);
                            ctD["SoLuongGoc"] = reader2["SoLuongGoc"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader2["SoLuongGoc"]);
                            ctD["MaLoaiDonViGoc"] = reader2["MaLoaiDonViGoc"] == DBNull.Value ? null : reader2["MaLoaiDonViGoc"].ToString();
                            ctD["MaLoaiDonViMoi"] = reader2["MaLoaiDonViMoi"] == DBNull.Value ? null : reader2["MaLoaiDonViMoi"].ToString();
                            ctList.Add(ct);
                        }
                    }
                }

                var outD = (IDictionary<string, object>)output;
                // Enrich header with employee name and detail rows with unit names
                try
                {
                    // collect MaNV from header
                    var maNv = headerDict.ContainsKey("MaNV") ? headerDict["MaNV"] as string : null;

                    // collect unit codes and MaThuoc from details
                    var unitCodes = new List<string>();
                    var maThuocCodes = new List<string>();
                    foreach (var ct in ctList)
                    {
                        var dict = (IDictionary<string, object>)ct;
                        if (dict.TryGetValue("MaLoaiDonViGoc", out var mg) && mg is string mgv && !string.IsNullOrWhiteSpace(mgv)) unitCodes.Add(mgv);
                        if (dict.TryGetValue("MaLoaiDonViMoi", out var mm) && mm is string mmv && !string.IsNullOrWhiteSpace(mmv)) unitCodes.Add(mmv);
                        if (dict.TryGetValue("MaThuoc", out var mt) && mt is string mtv && !string.IsNullOrWhiteSpace(mtv)) maThuocCodes.Add(mtv);
                    }

                    var distinctUnits = unitCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    // fetch employee name
                    if (!string.IsNullOrWhiteSpace(maNv))
                    {
                        var nv = await ctx.Set<Model.NhanVien>().AsNoTracking().FirstOrDefaultAsync(n => n.MaNV == maNv);
                        if (nv != null) headerDict["NhanVienName"] = nv.HoTen;
                    }

                    // fetch unit names
                    if (distinctUnits.Any())
                    {
                        var unitMap = await ctx.Set<Model.Thuoc.LoaiDonVi>().AsNoTracking()
                            .Where(u => distinctUnits.Contains(u.MaLoaiDonVi))
                            .ToDictionaryAsync(u => u.MaLoaiDonVi ?? string.Empty, u => u.TenLoaiDonVi ?? string.Empty);

                        foreach (var ct in ctList)
                        {
                            var dict = (IDictionary<string, object>)ct;
                            if (dict.TryGetValue("MaLoaiDonViGoc", out var mg) && mg is string mgv && !string.IsNullOrWhiteSpace(mgv))
                            {
                                dict["MaLoaiDonViGocName"] = unitMap.ContainsKey(mgv) ? unitMap[mgv] : null;
                            }
                            else dict["MaLoaiDonViGocName"] = null;

                            if (dict.TryGetValue("MaLoaiDonViMoi", out var mm) && mm is string mmv && !string.IsNullOrWhiteSpace(mmv))
                            {
                                dict["MaLoaiDonViMoiName"] = unitMap.ContainsKey(mmv) ? unitMap[mmv] : null;
                            }
                            else dict["MaLoaiDonViMoiName"] = null;
                        }
                    }

                        // fetch TenThuoc for MaThuoc values
                        var distinctMaThuoc = maThuocCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        if (distinctMaThuoc.Any())
                        {
                            var thuocMap = await ctx.Set<Model.Thuoc.Thuoc>().AsNoTracking()
                                .Where(t => distinctMaThuoc.Contains(t.MaThuoc))
                                .ToDictionaryAsync(t => t.MaThuoc ?? string.Empty, t => t.TenThuoc ?? string.Empty);

                            foreach (var ct in ctList)
                            {
                                var dict = (IDictionary<string, object>)ct;
                                if (dict.TryGetValue("MaThuoc", out var mt) && mt is string mtv && !string.IsNullOrWhiteSpace(mtv))
                                {
                                    dict["TenThuoc"] = thuocMap.ContainsKey(mtv) ? thuocMap[mtv] : null;
                                }
                                else dict["TenThuoc"] = null;
                            }
                        }
                }
                catch
                {
                    // non-fatal: if enrichment fails, continue returning base data
                }

                outD["Phieu"] = header;
                outD["ChiTiets"] = ctList;
            }

            return output;
        }

        // in-memory queue for enqueue-by-ma requests
        private static string GenMaPhieuQD()
        {
            return "QD" + DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString();
        }

        private static string GenMaLo()
        {
            return "LO" + DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(10, 99).ToString();
        }

        /// <summary>
        /// Quick convert by MaThuoc (single package). unitsPerPackageOverride can be null to use THUOC.SoLuong.
        /// Returns MaPhieuQD and MaLoMoi.
        /// </summary>
        public async Task<(string MaPhieuQD, string MaLoMoi, string MaLoaiDonViGoc, string MaLoaiDonViMoi, int SoLuongGoc, int SoLuongQuyDoi, int TyLeQuyDoi)> QuickConvertByMaAsync(string maThuoc, int? unitsPerPackageOverride = null, string? maLoaiDonViMoi = null, DateTime? hanSuDungMoi = null, string? maLoGoc = null, string? maLoaiDonViGoc = null, bool ignoreTrangThaiSeal = false)
        {
            if (string.IsNullOrWhiteSpace(maThuoc)) throw new ArgumentException("maThuoc is required");

            var ctx = _repo.Context;
            var thuoc = await ctx.Set<Model.Thuoc.Thuoc>().AsNoTracking().FirstOrDefaultAsync(t => t.MaThuoc == maThuoc);
            if (thuoc == null) throw new KeyNotFoundException("Thuoc not found by code");

            // determine a sensible default for unitsPerPackage but do NOT fail here.
            // The precise conversion ratio will be computed from GiaThuoc entries for the given MaThuoc
            // after resolving the source/target unit codes (see below). This avoids forcing callers to
            // provide maLoaiDonViMoi when a ratio can be computed from GiaThuoc for the same medicine.
            int unitsPerPackage = 1;
            if (unitsPerPackageOverride.HasValue && unitsPerPackageOverride.Value > 0)
            {
                unitsPerPackage = unitsPerPackageOverride.Value;
            }
            else
            {
                // try to use GiaThuoc if caller provided the target unit code
                if (!string.IsNullOrEmpty(maLoaiDonViMoi))
                {
                    var gia = await ctx.GiaThuocs.AsNoTracking().FirstOrDefaultAsync(g => g.MaThuoc == maThuoc && g.MaLoaiDonVi == maLoaiDonViMoi);
                    if (gia != null && gia.SoLuong > 0)
                    {
                        unitsPerPackage = gia.SoLuong;
                    }
                }

                // otherwise fallback to a safe default of 1
                if (unitsPerPackage <= 0)
                {
                    unitsPerPackage = 1;
                }
            }

            // find source lot: prefer provided MaLoGoc if given, otherwise pick nearest expiry sealed lot
            Model.Kho.TonKho? candidateLot = null;
            if (!string.IsNullOrWhiteSpace(maLoGoc))
            {
                candidateLot = await ctx.TonKhos.Where(t => t.MaLo == maLoGoc).FirstOrDefaultAsync();
                if (candidateLot == null) throw new KeyNotFoundException($"TonKho '{maLoGoc}' not found");
                if (candidateLot.MaThuoc != maThuoc) throw new InvalidOperationException("Provided MaLoGoc does not belong to the specified MaThuoc.");
                if (candidateLot.TrangThaiSeal && !ignoreTrangThaiSeal) throw new InvalidOperationException("Provided MaLoGoc has already been split (TrangThaiSeal = 1)");
                if (candidateLot.SoLuongCon < 1) throw new InvalidOperationException("Provided MaLoGoc does not have sufficient quantity");
            }
            else
            {
                // If a source unit code was provided, prefer selecting a lot that has the same MaLoaiDonViTinh
                // so we consume from the lot that is in the original unit (e.g., consume from 'LDV003' lots when
                // converting from LDV003 -> LDV001). If none found, fall back to earliest expiry lot for the MaThuoc.
                if (!string.IsNullOrWhiteSpace(maLoaiDonViGoc))
                {
                    candidateLot = await ctx.TonKhos
                        .Where(t => t.MaThuoc == maThuoc && t.MaLoaiDonViTinh == maLoaiDonViGoc && (ignoreTrangThaiSeal || !t.TrangThaiSeal) && t.SoLuongCon >= 1)
                        .OrderBy(t => t.HanSuDung)
                        .FirstOrDefaultAsync();
                }

                if (candidateLot == null)
                {
                    candidateLot = await ctx.TonKhos
                        .Where(t => t.MaThuoc == maThuoc && (ignoreTrangThaiSeal || !t.TrangThaiSeal) && t.SoLuongCon >= 1)
                        .OrderBy(t => t.HanSuDung)
                        .FirstOrDefaultAsync();
                }
            }
            if (candidateLot == null) throw new InvalidOperationException("No sealed lot with sufficient quantity found");

            // perform single-lot conversion using a transaction
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var ctx2 = _repo.Context;

                // lock the specific lot row to avoid race conditions
                var tk = await ctx2.TonKhos
                    .FromSqlRaw("SELECT * FROM TON_KHO WITH (UPDLOCK, ROWLOCK) WHERE MaLo = {0}", candidateLot.MaLo)
                    .AsTracking()
                    .FirstOrDefaultAsync();

                if (tk == null) throw new KeyNotFoundException($"TonKho '{candidateLot.MaLo}' not found");
                if (tk.TrangThaiSeal && !ignoreTrangThaiSeal) throw new InvalidOperationException("Lot is already split (TrangThaiSeal = 1)");
                if (tk.SoLuongCon < 1) throw new InvalidOperationException("Not enough quantity in source lot");

                // consume one source package
                tk.SoLuongCon -= 1;

                var maPhieu = GenMaPhieuQD();
                var maLoMoi = GenMaLo();

                // determine MaNV to record on the phieu
                var anyNv = await ctx2.Set<Model.NhanVien>().AsNoTracking().FirstOrDefaultAsync();
                var maNVToRecord = anyNv?.MaNV;
                if (string.IsNullOrEmpty(maNVToRecord)) throw new InvalidOperationException("No NhanVien found to attribute quick convert phieu.");

                    // Determine real unit codes. Prefer explicit maLoaiDonViGoc argument when provided.
                    var maLoaiDonViGocResolved = !string.IsNullOrWhiteSpace(maLoaiDonViGoc) ? maLoaiDonViGoc : tk.MaLoaiDonViTinh;
                    var maLoaiDonViMoiResolved = string.IsNullOrEmpty(maLoaiDonViMoi) ? maLoaiDonViGocResolved : maLoaiDonViMoi;

                // get size (base unit count) for source and target units from GiaThuoc.SoLuong
                var giaGoc = await ctx2.GiaThuocs.AsNoTracking().FirstOrDefaultAsync(g => g.MaThuoc == maThuoc && g.MaLoaiDonVi == maLoaiDonViGocResolved);
                var giaMoi = await ctx2.GiaThuocs.AsNoTracking().FirstOrDefaultAsync(g => g.MaThuoc == maThuoc && g.MaLoaiDonVi == maLoaiDonViMoiResolved);
                var sizeGoc = (giaGoc?.SoLuong > 0) ? giaGoc.SoLuong : 1;
                var sizeMoi = (giaMoi?.SoLuong > 0) ? giaMoi.SoLuong : unitsPerPackage; // fallback to unitsPerPackage

                // Validation: reject conversions where the target unit's SoLuong is less than the source unit's SoLuong.
                // Example: GiaThuoc[MaLoaiDonViMoi = 'HOP'].SoLuong = 1 and GiaThuoc[MaLoaiDonViGoc = 'VIEN'].SoLuong = 100
                // means converting from VIEN -> HOP would be invalid because 100 VIEN == 1 HOP.
                if (sizeMoi < sizeGoc)
                {
                    throw new InvalidOperationException($"Invalid conversion: target unit '{maLoaiDonViMoiResolved}' has SoLuong {sizeMoi} which is less than source unit '{maLoaiDonViGocResolved}' SoLuong {sizeGoc}. Conversion from smaller unit to larger packaging is not allowed.");
                }
                if (sizeGoc <= 0) sizeGoc = 1; // guard against divide-by-zero
                double ratio = (double)sizeMoi / (double)sizeGoc;
                int tyLeQuyDoi;
                try { checked { tyLeQuyDoi = (int)Math.Round(ratio); } }
                catch { tyLeQuyDoi = 1; }
                if (tyLeQuyDoi <= 0) tyLeQuyDoi = 1;
                int soLuongGoc = 1;
                int soLuongQuyDoi = soLuongGoc * tyLeQuyDoi;
                var targetHanSuDung = hanSuDungMoi ?? tk.HanSuDung;
                var existing = await ctx2.TonKhos
                    .Where(t => t.MaThuoc == tk.MaThuoc && t.HanSuDung == targetHanSuDung && t.MaLoaiDonViTinh == maLoaiDonViMoiResolved)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    existing.SoLuongNhap += soLuongQuyDoi;
                    existing.SoLuongCon += soLuongQuyDoi;
                    maLoMoi = existing.MaLo;
                }
                else
                {
                    var newLot = new TonKho
                    {
                        MaLo = maLoMoi,
                        MaThuoc = tk.MaThuoc,
                        HanSuDung = targetHanSuDung,
                        TrangThaiSeal = true,
                        MaLoaiDonViTinh = maLoaiDonViMoiResolved,
                        SoLuongNhap = soLuongQuyDoi,
                        SoLuongCon = soLuongQuyDoi,
                        GhiChu = $"Tách từ {tk.MaLo} (Phiếu {maPhieu})"
                    };
                    await ctx2.TonKhos.AddAsync(newLot);
                }

                // persist source lot update and possibly new TON_KHO so MaLoMoi exists
                await ctx2.SaveChangesAsync();

                // insert PHIEU_QUY_DOI
                await ctx2.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PHIEU_QUY_DOI (MaPhieuQD, NgayQuyDoi, MaNV, GhiChu) VALUES ({0}, {1}, {2}, {3})",
                    maPhieu, DateTime.Now, maNVToRecord, $"Quick convert: {maThuoc}");

                // insert CT_PHIEU_QUY_DOI with correct conversion values
                await ctx2.Database.ExecuteSqlRawAsync(
                    "INSERT INTO CT_PHIEU_QUY_DOI (MaPhieuQD, MaLoGoc, MaLoMoi, MaThuoc, SoLuongQuyDoi, TyLeQuyDoi, SoLuongGoc, MaLoaiDonViGoc, MaLoaiDonViMoi) VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8})",
                    maPhieu, tk.MaLo, maLoMoi, tk.MaThuoc, soLuongQuyDoi, tyLeQuyDoi, soLuongGoc, maLoaiDonViGocResolved, maLoaiDonViMoiResolved);

                await tx.CommitAsync();
                return (maPhieu, maLoMoi, maLoaiDonViGocResolved ?? string.Empty, maLoaiDonViMoiResolved ?? string.Empty, soLuongGoc, soLuongQuyDoi, tyLeQuyDoi);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        /// <summary>
        /// Create a PHIEU_QUY_DOI for a batch of items (multiple source lots). Returns MaPhieuQD and list of MaLoMoi created.
        /// </summary>
        public async Task<(string MaPhieuQD, List<string> MaLoMoi)> CreatePhieuQuyDoiBatchAsync(PhieuQuyDoiBatchCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Items == null || !dto.Items.Any()) throw new ArgumentException("Items list is required and cannot be empty");
            // New implementation: items contain MaThuoc (medicine code). For each item, consume sealed lots (TrangThaiSeal = false)
            // in HSD order until SoLuongGoc is satisfied. This may produce multiple CT rows per item.
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var ctx = _repo.Context;

                var ctRows = new List<(string MaLoGoc, string MaLoMoi, string MaThuoc, int SoLuongQuyDoi, int TyLeQuyDoi, int SoLuongGoc, string MaLoaiDonViGoc, string MaLoaiDonViMoi)>();
                var createdMaLo = new List<string>();

                foreach (var item in dto.Items)
                {
                    if (item == null) throw new ArgumentException("Item cannot be null");
                    if (string.IsNullOrEmpty(item.MaThuoc)) throw new ArgumentException("MaThuoc is required for every item");
                    if (item.SoLuongGoc <= 0) throw new ArgumentException("SoLuongGoc must be > 0 for every item");

                    // resolve units per package from THUOC when not provided
                    var thuoc = await ctx.Set<Model.Thuoc.Thuoc>().AsNoTracking().FirstOrDefaultAsync(t => t.MaThuoc == item.MaThuoc);
                    if (thuoc == null) throw new KeyNotFoundException($"Thuoc not found for MaThuoc {item.MaThuoc}");
                    int unitsPerPackage = item.TyLeQuyDoi.HasValue && item.TyLeQuyDoi.Value > 0 ? item.TyLeQuyDoi.Value : throw new InvalidOperationException("TyLeQuyDoi (units per package) is required when Thuoc.SoLuong is not available.");

                    // collect candidate lots for this MaThuoc (sealed packages still in sealed state i.e., TrangThaiSeal == false)
                    var candidateLots = await ctx.TonKhos
                        .Where(t => t.MaThuoc == item.MaThuoc && !t.TrangThaiSeal && t.SoLuongCon > 0)
                        .OrderBy(t => t.HanSuDung)
                        .ToListAsync();

                    var totalAvailable = candidateLots.Sum(l => l.SoLuongCon);
                    if (totalAvailable < item.SoLuongGoc) throw new InvalidOperationException($"Not enough quantity across all sealed lots to fulfill MaThuoc {item.MaThuoc}");

                    int remaining = item.SoLuongGoc;
                    foreach (var lot in candidateLots)
                    {
                        if (remaining <= 0) break;

                        var take = Math.Min(remaining, lot.SoLuongCon);

                        // re-lock the specific lot row before mutating
                        var tk = await ctx.TonKhos.FromSqlRaw("SELECT * FROM TON_KHO WITH (UPDLOCK, ROWLOCK) WHERE MaLo = {0}", lot.MaLo).AsTracking().FirstOrDefaultAsync();
                        if (tk == null) throw new KeyNotFoundException($"TonKho '{lot.MaLo}' not found during consumption");
                        if (tk.TrangThaiSeal) throw new InvalidOperationException($"Lot {lot.MaLo} is already split (TrangThaiSeal = 1)");
                        if (tk.SoLuongCon < take) throw new InvalidOperationException($"Concurrent update reduced availability for lot {lot.MaLo}");

                        // decrement source lot by number of packages
                        tk.SoLuongCon -= take;

                        // compute loose units
                        int soLuongQuyDoi;
                        try { checked { soLuongQuyDoi = take * unitsPerPackage; } }
                        catch (OverflowException ex) { throw new InvalidOperationException("Quantity overflow during conversion", ex); }

                        var targetDonVi = string.IsNullOrEmpty(item.MaLoaiDonViMoi) ? "Viên" : item.MaLoaiDonViMoi;

                        // decide expiry for the new lot (explicit override per item, otherwise use source lot HSD)
                        var targetHanSuDung = item.HanSuDungMoi ?? tk.HanSuDung;

                        // check for existing aggregated lot with same MaThuoc, HanSuDung, MaLoaiDonViTinh
                        var existing = await ctx.TonKhos
                            .Where(t => t.MaThuoc == tk.MaThuoc && t.HanSuDung == targetHanSuDung && t.MaLoaiDonViTinh == targetDonVi)
                            .FirstOrDefaultAsync();

                        string maLoMoi;
                        if (existing != null)
                        {
                            existing.SoLuongNhap += soLuongQuyDoi;
                            existing.SoLuongCon += soLuongQuyDoi;
                            maLoMoi = existing.MaLo;
                        }
                        else
                        {
                            maLoMoi = GenMaLo();
                            var newLot = new TonKho
                            {
                                MaLo = maLoMoi,
                                MaThuoc = tk.MaThuoc,
                                HanSuDung = targetHanSuDung,
                                TrangThaiSeal = true,
                                MaLoaiDonViTinh = targetDonVi,
                                SoLuongNhap = soLuongQuyDoi,
                                SoLuongCon = soLuongQuyDoi,
                                GhiChu = $"Tách từ {tk.MaLo} (Phiếu ... pending)"
                            };
                            await ctx.TonKhos.AddAsync(newLot);
                        }

                        // record CT row for this consumed piece (use MaLoaiDonViGoc/MaLoaiDonViMoi column semantics)
                        ctRows.Add((tk.MaLo, maLoMoi, tk.MaThuoc, soLuongQuyDoi, unitsPerPackage, take, tk.MaLoaiDonViTinh, targetDonVi));
                        if (!createdMaLo.Contains(maLoMoi)) createdMaLo.Add(maLoMoi);

                        remaining -= take;
                    }
                }

                // persist all changes (source lot decreases and new/aggregated TON_KHO rows)
                await ctx.SaveChangesAsync();

                // generate MaPhieu and insert PHIEU_QUY_DOI
                var maPhieu = GenMaPhieuQD();
                string? maNVToRecord = dto.MaNV;
                if (string.IsNullOrEmpty(maNVToRecord))
                {
                    var anyNv = await ctx.Set<Model.NhanVien>().AsNoTracking().FirstOrDefaultAsync();
                    maNVToRecord = anyNv?.MaNV;
                }
                if (string.IsNullOrEmpty(maNVToRecord)) throw new InvalidOperationException("No MaNV provided and no NhanVien found in database to attribute the phieu.");

                await ctx.Database.ExecuteSqlRawAsync(
                    "INSERT INTO PHIEU_QUY_DOI (MaPhieuQD, NgayQuyDoi, MaNV, GhiChu) VALUES ({0}, {1}, {2}, {3})",
                    maPhieu, DateTime.Now, maNVToRecord, dto.GhiChu ?? string.Empty);

                // insert CT rows
                foreach (var r in ctRows)
                {
                    await ctx.Database.ExecuteSqlRawAsync(
                        "INSERT INTO CT_PHIEU_QUY_DOI (MaPhieuQD, MaLoGoc, MaLoMoi, MaThuoc, SoLuongQuyDoi, TyLeQuyDoi, SoLuongGoc, MaLoaiDonViGoc, MaLoaiDonViMoi) VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8})",
                        maPhieu, r.MaLoGoc, r.MaLoMoi, r.MaThuoc, r.SoLuongQuyDoi, r.TyLeQuyDoi, r.SoLuongGoc, r.MaLoaiDonViGoc, r.MaLoaiDonViMoi);
                }

                await tx.CommitAsync();
                return (maPhieu, createdMaLo);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Return TonKho projection for a given MaLo.
        /// </summary>
        public async Task<dynamic> GetTonKhoAsync(string maLo)
        {
            if (string.IsNullOrWhiteSpace(maLo)) return null;
            var ctx = _repo.Context;
            var tk = await ctx.TonKhos
                .Where(t => t.MaLo == maLo)
                .Select(t => new { t.MaLo, t.MaThuoc, t.MaLoaiDonViTinh, t.SoLuongNhap, t.SoLuongCon, t.HanSuDung, t.GhiChu })
                .AsNoTracking()
                .FirstOrDefaultAsync();
            return tk;
        }
    }
}
