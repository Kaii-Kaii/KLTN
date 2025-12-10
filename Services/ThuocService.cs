using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Model.Thuoc;
using BE_QLTiemThuoc.Repositories;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BE_QLTiemThuoc.Services
{
    public class ThuocService
    {
        private readonly ThuocRepository _repo;
        private readonly Cloudinary _cloudinary;
        private readonly IWebHostEnvironment _env;

        public ThuocService(ThuocRepository repo, Cloudinary cloudinary, IWebHostEnvironment env)
        {
            _repo = repo;
            _cloudinary = cloudinary;
            _env = env;
        }

        private static string? ExtractFileNameFromUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            try
            {
                var s = input.Trim();
                if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(s);
                    s = uri.LocalPath ?? s;
                }
                var noQuery = s.Split('?')[0];
                noQuery = noQuery.Trim('/');
                var file = Path.GetFileName(noQuery);
                return string.IsNullOrEmpty(file) ? null : file;
            }
            catch
            {
                try
                {
                    var t = input.Split('?')[0].Trim('/');
                    var f = Path.GetFileName(t);
                    return string.IsNullOrEmpty(f) ? null : f;
                }
                catch
                {
                    return null;
                }
            }
        }

        public async Task<object> GetTopLoaiThuocAsync()
        {
            var ctx = _repo.Context;
            var thuocGroup = await ctx.Thuoc
                .GroupBy(t => t.MaLoaiThuoc)
                .Select(g => new
                {
                    MaLoaiThuoc = g.Key,
                    SoLuongThuoc = g.Count()
                })
                .ToListAsync();

            var loaiThuocList = await ctx.LoaiThuoc
                .Select(l => new { l.MaLoaiThuoc, l.TenLoaiThuoc, l.MaNhomLoai })
                .ToListAsync();
            var nhomList = await ctx.NhomLoai.ToListAsync();

            var thongKeList = loaiThuocList
                .Select(loai =>
                {
                    var thuocInfo = thuocGroup.FirstOrDefault(x => x.MaLoaiThuoc == loai.MaLoaiThuoc);
                    var nhom = nhomList.FirstOrDefault(n => n.MaNhomLoai == loai.MaNhomLoai);
                    return (object)new LoaiThuocThongKe
                    {
                        MaLoaiThuoc = loai.MaLoaiThuoc,
                        TenLoaiThuoc = loai.TenLoaiThuoc,
                        MaNhomLoai = loai.MaNhomLoai,
                        TenNhomLoai = nhom?.TenNhomLoai,
                        SoLuongThuoc = thuocInfo?.SoLuongThuoc ?? 0
                    };
                })
                .OrderByDescending(x => ((LoaiThuocThongKe)x).SoLuongThuoc)
                .ToList();

            return thongKeList;
        }

        public Task<object> GetThuocAsync()
        {
            return _repo.Context.Thuoc
                .Select(t => new
                {
                    t.MaThuoc,
                    t.Code,
                    t.MaLoaiThuoc,
                    t.TenThuoc,
                    t.ThanhPhan, 
                    t.MoTa,
                    t.UrlAnh,
                })
                .ToListAsync()
                .ContinueWith(t => (object)t.Result);
        }
        // GET: list of Thuoc aggregated by available lots in TonKho (only lots with SoLuongCon>0 and TrangThaiSeal==0)
        // Only show GiaThuoc where TrangThai == true (active prices)
        public async Task<object> GetListThuocTonKhoAsync()
        {
            var ctx = _repo.Context;
            var grouped = ctx.TonKhos
                .Where(tk => tk.SoLuongCon > 0 && !tk.TrangThaiSeal)
                .GroupBy(tk => tk.MaThuoc)
                .Select(g => new
                {
                    MaThuoc = g.Key,
                    TongSoLuongCon = g.Sum(tk => tk.SoLuongCon)
                });

            var result = await grouped
                .Join(ctx.Thuoc,
                      g => g.MaThuoc,
                      t => t.MaThuoc,
                      (g, t) => new
                      {
                          maThuoc = t.MaThuoc,
                          maLoaiThuoc = t.MaLoaiThuoc,
                          tenThuoc = t.TenThuoc,
                          thanhPhan = t.ThanhPhan,
                          moTa = t.MoTa,
                          urlAnh = t.UrlAnh,
                          tongSoLuongCon = g.TongSoLuongCon,
                          GiaThuocs = ctx.GiaThuocs
                                        .Where(x => x.MaThuoc == t.MaThuoc && x.TrangThai)
                                        .Select(x => new
                                        {
                                            x.MaGiaThuoc,
                                            x.MaLoaiDonVi,
                                            TenLoaiDonVi = ctx.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == x.MaLoaiDonVi).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                                            x.SoLuong,
                                            x.DonGia,
                                            x.TrangThai,
                                            SoLuongCon = ctx.TonKhos.Where(tk => tk.MaThuoc == t.MaThuoc && tk.MaLoaiDonViTinh == x.MaLoaiDonVi && tk.SoLuongCon > 0).Sum(tk => (int?)tk.SoLuongCon) ?? 0
                                        })
                                        .ToList()
                      })
                .Where(x => x.GiaThuocs.Count > 0)
                .OrderBy(x => x.tenThuoc)
                .ToListAsync();

            return result;
        }

        public Task<object> GetThuocByLoaiAsync(string maLoaiThuoc)
        {
            var ctx = _repo.Context;
            return ctx.Thuoc
                .Where(t => t.MaLoaiThuoc == maLoaiThuoc)
                .Select(t => new
                {
                    t.MaThuoc,
                    t.MaLoaiThuoc,
                    t.TenThuoc,
                    t.MoTa,
                    t.UrlAnh,
                    TenNCC = ctx.NhaCungCaps.Where(n => n.MaNCC == t.MaNCC).Select(n => n.TenNCC).FirstOrDefault(),
                    GiaThuocs = ctx.GiaThuocs
                                .Where(x => x.MaThuoc == t.MaThuoc)
                                .Select(x => new
                                {
                                    x.MaGiaThuoc,
                                    x.MaLoaiDonVi,
                                    TenLoaiDonVi = ctx.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == x.MaLoaiDonVi).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                                    x.SoLuong,
                                    x.DonGia,
                                    x.TrangThai,
                                    SoLuongCon = ctx.TonKhos.Where(tk => tk.MaThuoc == t.MaThuoc && tk.MaLoaiDonViTinh == x.MaLoaiDonVi && tk.SoLuongCon > 0).Sum(tk => (int?)tk.SoLuongCon) ?? 0
                                })
                                .ToList()
                })
                .ToListAsync()
                .ContinueWith(t => (object)t.Result);
        }

        // lightweight: return only MaThuoc and TenThuoc for a given MaLoaiThuoc
        public Task<object> GetThuocNamesByLoaiAsync(string maLoaiThuoc)
        {
            var ctx = _repo.Context;
            return ctx.Thuoc
                .Where(t => t.MaLoaiThuoc == maLoaiThuoc)
                .Select(t => new
                {
                    t.MaThuoc,
                    t.TenThuoc,
                    t.UrlAnh
                })
                .ToListAsync()
                .ContinueWith(t => (object)t.Result!);
        }

        // GET: api/Thuoc/ByLoaiTonKho/{maLoaiThuoc}
        // Return Thuoc of a given MaLoai that have available stock in TonKho (SoLuongCon>0 and not sealed)
        public Task<object> GetThuocByLoaiTonKhoAsync(string maLoaiThuoc)
        {
            var ctx = _repo.Context;

            return ctx.Thuoc
                .Where(t => t.MaLoaiThuoc == maLoaiThuoc
                            && ctx.TonKhos.Any(tk => tk.MaThuoc == t.MaThuoc && tk.SoLuongCon > 0 && !tk.TrangThaiSeal))
                .Select(t => new
                {
                    t.MaThuoc,
                    t.MaLoaiThuoc,
                    t.TenThuoc,
                    t.MoTa,
                    t.UrlAnh,
                    TenNCC = ctx.NhaCungCaps.Where(n => n.MaNCC == t.MaNCC).Select(n => n.TenNCC).FirstOrDefault(),
                    GiaThuocs = ctx.GiaThuocs
                                .Where(x => x.MaThuoc == t.MaThuoc && x.TrangThai
                                             && ctx.TonKhos.Any(tk => tk.MaThuoc == t.MaThuoc && tk.MaLoaiDonViTinh == x.MaLoaiDonVi && tk.SoLuongCon > 0 && !tk.TrangThaiSeal))
                                .Select(x => new
                                {
                                    x.MaGiaThuoc,
                                    x.MaLoaiDonVi,
                                    TenLoaiDonVi = ctx.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == x.MaLoaiDonVi).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                                    x.SoLuong,
                                    x.DonGia,
                                    x.TrangThai,
                                    SoLuongCon = ctx.TonKhos.Where(tk => tk.MaThuoc == t.MaThuoc && tk.MaLoaiDonViTinh == x.MaLoaiDonVi && tk.SoLuongCon > 0).Sum(tk => (int?)tk.SoLuongCon) ?? 0
                                })
                                .ToList()
                })
                .Where(t => t.GiaThuocs.Count > 0)
                .ToListAsync()
                .ContinueWith(t => (object)t.Result);
        }

        public Task<object?> GetThuocByIdAsync(string maThuoc)
        {
            var ctx = _repo.Context;
            return ctx.Thuoc
                .Where(t => t.MaThuoc == maThuoc)
                .Select(t => new
                {
                    t.MaThuoc,
                    t.Code,
                    t.MaLoaiThuoc,
                    TenLoaiThuoc = ctx.LoaiThuoc.Where(lt => lt.MaLoaiThuoc == t.MaLoaiThuoc).Select(lt => lt.TenLoaiThuoc).FirstOrDefault(),
                    t.TenThuoc,
                    t.ThanhPhan,
                    t.MoTa,
                    t.CongDung,
                    t.CachDung,
                    t.LuuY,
                    t.UrlAnh,
                    t.MaNCC,
                    TenNCC = ctx.NhaCungCaps.Where(n => n.MaNCC == t.MaNCC).Select(n => n.TenNCC).FirstOrDefault(),
                    GiaThuocs = ctx.GiaThuocs
                                .Where(x => x.MaThuoc == t.MaThuoc)
                                .Select(x => new
                                {
                                    x.MaGiaThuoc,
                                    x.MaLoaiDonVi,
                                    TenLoaiDonVi = ctx.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == x.MaLoaiDonVi).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                                    x.SoLuong,
                                    x.DonGia,
                                    x.TrangThai,
                                    SoLuongCon = ctx.TonKhos.Where(tk => tk.MaThuoc == t.MaThuoc && tk.MaLoaiDonViTinh == x.MaLoaiDonVi && tk.SoLuongCon > 0 ).Sum(tk => (int?)tk.SoLuongCon) ?? 0
                                })
                                .ToList()
                })
                .FirstOrDefaultAsync()
                .ContinueWith(t => (object?)t.Result);
        }
        // GET: all GiaThuoc rows for a given MaThuoc with computed SoLuongCon and TenLoaiDonVi
        // Also returns the nearest expiration date (HanSuDung) among available lots for this MaThuoc
        public async Task<object> GetGiaThuocsByMaThuocAsync(string maThuoc)
        {
            var ctx = _repo.Context;
            var today = DateTime.Now.Date;

            // Fetch all GiaThuoc rows for the MaThuoc
            var giaEntities = await ctx.GiaThuocs
                .Where(g => g.MaThuoc == maThuoc)
                .ToListAsync();

            // Precompute nearest HanSuDung per MaLoaiDonViTinh among available lots (SoLuongCon>0)
            var nearestByUnit = await ctx.TonKhos
                .Where(tk => tk.MaThuoc == maThuoc && tk.SoLuongCon > 0 && tk.HanSuDung > today)
                .GroupBy(tk => tk.MaLoaiDonViTinh)
                .Select(g => new { MaLoaiDonVi = g.Key, Nearest = g.Min(tk => (DateTime?)tk.HanSuDung) })
                .ToListAsync();

            var giaList = giaEntities.Select(x => new
            {
                x.MaGiaThuoc,
                x.MaLoaiDonVi,
                TenLoaiDonVi = ctx.Set<LoaiDonVi>().Where(d => d.MaLoaiDonVi == x.MaLoaiDonVi).Select(d => d.TenLoaiDonVi).FirstOrDefault(),
                x.SoLuong,
                x.DonGia,
                x.TrangThai,
                SoLuongCon = ctx.TonKhos.Where(tk => tk.MaThuoc == x.MaThuoc && tk.MaLoaiDonViTinh == x.MaLoaiDonVi && tk.SoLuongCon > 0 && tk.HanSuDung > today).Sum(tk => (int?)tk.SoLuongCon) ?? 0,
                NearestHanSuDung = nearestByUnit.FirstOrDefault(n => n.MaLoaiDonVi == x.MaLoaiDonVi)?.Nearest
            }).ToList();

            return new { GiaThuocs = giaList }!;
        }

        public Task<List<LoaiDonVi>> GetLoaiDonViAsync()
        {
            return _repo.Context.LoaiDonVi.ToListAsync();
        }

        public async Task<Thuoc> CreateThuocAsync(ThuocDto thuocDto, HttpRequest? request = null)
        {
            if (thuocDto == null) throw new ArgumentNullException(nameof(thuocDto));

            string newMaThuoc;
            do
            {
                newMaThuoc = "T" + Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper();
            } while (await _repo.AnyByMaThuocAsync(newMaThuoc));

            string? imageUrl = null;
            string? providedUrlFromForm = null;
            try
            {
                if (request != null && request.HasFormContentType)
                {
                    var form = await request.ReadFormAsync();
                    if (thuocDto != null && string.IsNullOrWhiteSpace(thuocDto.UrlAnh) && form.TryGetValue("UrlAnh", out var v))
                        providedUrlFromForm = v.ToString();
                    if (string.IsNullOrWhiteSpace(providedUrlFromForm))
                    {
                        var foundKey = form.Keys.FirstOrDefault(k => string.Equals(k, "urlanh", StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(foundKey)) providedUrlFromForm = form[foundKey].ToString();
                    }
                }
            }
            catch { }

            if (thuocDto.FileAnh?.Length > 0)
            {
                if (thuocDto.FileAnh.Length > 5242880) throw new Exception("Kích thước file ảnh không được vượt quá 5MB.");

                var tagsList = new List<string> { "thuoc" };
                if (!string.IsNullOrEmpty(thuocDto.MaLoaiThuoc)) tagsList.Add(thuocDto.MaLoaiThuoc.Trim().ToLower());
                string tagsString = string.Join(",", tagsList);

                using (var stream = thuocDto.FileAnh.OpenReadStream())
                {
                    var originalFileName = Path.GetFileName(thuocDto.FileAnh.FileName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName) ?? "file";
                    var ext = Path.GetExtension(originalFileName) ?? "";
                    string SanitizeForPublicId(string s) => Regex.Replace(s, @"[^A-Za-z0-9_\-]", "_");

                    var baseCandidate = SanitizeForPublicId(nameWithoutExt);
                    string candidateStored = baseCandidate + ext;
                    string candidatePublicBase = baseCandidate;
                    int suffix = 1;
                    while (await _repo.Context.Thuoc.AnyAsync(t => t.UrlAnh.ToLower() == candidateStored.ToLower()))
                    {
                        candidatePublicBase = SanitizeForPublicId($"{baseCandidate}_{suffix}");
                        candidateStored = candidatePublicBase + ext;
                        suffix++;
                    }

                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(thuocDto.FileAnh.FileName, stream),
                        Folder = "thuoc_images",
                        PublicId = $"thuoc/{candidatePublicBase}",
                        Overwrite = false,
                        Tags = tagsString
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception($"Lỗi Cloudinary: {uploadResult.Error?.Message}");

                    string finalFileName;
                    if (!string.IsNullOrEmpty(uploadResult.PublicId))
                    {
                        var lastSegment = uploadResult.PublicId.Contains('/') ? uploadResult.PublicId.Split('/').Last() : uploadResult.PublicId;
                        finalFileName = !string.IsNullOrEmpty(uploadResult.Format) ? lastSegment + "." + uploadResult.Format : candidateStored;
                    }
                    else if (uploadResult.SecureUrl != null)
                    {
                        finalFileName = Path.GetFileName(new Uri(uploadResult.SecureUrl.ToString()).LocalPath);
                    }
                    else
                    {
                        finalFileName = candidateStored;
                    }

                    imageUrl = finalFileName;
                }
            }
            else
            {
                var urlToUse = !string.IsNullOrWhiteSpace(thuocDto?.UrlAnh) ? thuocDto.UrlAnh : providedUrlFromForm;
                if (!string.IsNullOrWhiteSpace(urlToUse))
                {
                    if (urlToUse.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || urlToUse.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        imageUrl = urlToUse;
                    }
                    else
                    {
                        var extracted = ExtractFileNameFromUrl(urlToUse);
                        imageUrl = extracted ?? urlToUse;
                    }
                }
            }

            var thuoc = new Thuoc
            {
                MaThuoc = newMaThuoc,
                MaLoaiThuoc = thuocDto?.MaLoaiThuoc ?? string.Empty,
                TenThuoc = thuocDto?.TenThuoc ?? string.Empty,
                ThanhPhan = thuocDto?.ThanhPhan ?? string.Empty,
                MoTa = thuocDto?.MoTa ?? string.Empty,
                CongDung = thuocDto?.CongDung ?? string.Empty,
                CachDung = thuocDto?.CachDung ?? string.Empty,
                LuuY = thuocDto?.LuuY ?? string.Empty,
                MaNCC = thuocDto?.MaNCC ?? string.Empty,
                UrlAnh = imageUrl ?? string.Empty,
                Code = thuocDto?.Code ?? string.Empty
            };

            var ctx = _repo.Context;
            await using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                await _repo.AddAsync(thuoc);
                await _repo.SaveChangesAsync();

                if (thuocDto.GiaThuocs?.Count > 0)
                {
                    var existingCount = await ctx.GiaThuocs.CountAsync(x => x.MaThuoc == thuoc.MaThuoc);
                    var nextIndex = existingCount + 1;

                    string BuildMaGiaThuoc(string baseMaThuoc, int idx)
                    {
                        return "GT" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    }

                    foreach (var g in thuocDto.GiaThuocs)
                    {
                        var newMa = BuildMaGiaThuoc(newMaThuoc, nextIndex++);
                        var newRow = new GiaThuoc
                        {
                            MaGiaThuoc = newMa,
                            MaThuoc = newMaThuoc,
                            MaLoaiDonVi = g.MaLoaiDonVi,
                            SoLuong = g.SoLuong,
                            DonGia = g.DonGia,
                            TrangThai = g.TrangThai
                        };
                        await ctx.GiaThuocs.AddAsync(newRow);
                    }
                    await ctx.SaveChangesAsync();
                }

                await tx.CommitAsync();
                return thuoc;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Update
        public async Task<bool> UpdateThuocAsync(string id, ThuocDto thuocDto, HttpRequest? request = null)
        {
            if (thuocDto == null) throw new ArgumentNullException(nameof(thuocDto));
            // DTO no longer contains MaThuoc; use route `id` as the identifier for updates.

            var entity = await _repo.FindAsync(id);
            if (entity == null) throw new Exception("Không tìm thấy thuốc.");

            // Handle file upload similar to Create
            if (thuocDto.FileAnh?.Length > 0)
            {
                if (thuocDto.FileAnh.Length > 5242880) throw new Exception("Kích thước file ảnh không được vượt quá 5MB.");

                var tagsList = new List<string> { "thuoc" };
                if (!string.IsNullOrEmpty(thuocDto.MaLoaiThuoc)) tagsList.Add(thuocDto.MaLoaiThuoc.Trim().ToLower());
                string tagsString = string.Join(",", tagsList);

                using (var stream = thuocDto.FileAnh.OpenReadStream())
                {
                    var originalFileName = Path.GetFileName(thuocDto.FileAnh.FileName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName) ?? "file";
                    var ext = Path.GetExtension(originalFileName) ?? "";
                    string SanitizeForPublicId(string s) => Regex.Replace(s, @"[^A-Za-z0-9_\-]", "_");

                    var baseCandidate = SanitizeForPublicId(nameWithoutExt);
                    string candidateStored = baseCandidate + ext;
                    string candidatePublicBase = baseCandidate;
                    int suffix = 1;
                    while (await _repo.Context.Thuoc.AnyAsync(t => t.UrlAnh.ToLower() == candidateStored.ToLower()))
                    {
                        candidatePublicBase = SanitizeForPublicId($"{baseCandidate}_{suffix}");
                        candidateStored = candidatePublicBase + ext;
                        suffix++;
                    }

                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(thuocDto.FileAnh.FileName, stream),
                        Folder = "thuoc_images",
                        PublicId = $"thuoc/{candidatePublicBase}",
                        Overwrite = false,
                        Tags = tagsString
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception($"Lỗi Cloudinary: {uploadResult.Error?.Message}");

                    string finalFileName;
                    if (!string.IsNullOrEmpty(uploadResult.PublicId))
                    {
                        var lastSegment = uploadResult.PublicId.Contains('/') ? uploadResult.PublicId.Split('/').Last() : uploadResult.PublicId;
                        finalFileName = !string.IsNullOrEmpty(uploadResult.Format) ? lastSegment + "." + uploadResult.Format : candidateStored;
                    }
                    else if (uploadResult.SecureUrl != null)
                    {
                        finalFileName = Path.GetFileName(new Uri(uploadResult.SecureUrl.ToString()).LocalPath);
                    }
                    else
                    {
                        finalFileName = candidateStored;
                    }

                    entity.UrlAnh = finalFileName;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(thuocDto.UrlAnh))
                {
                    // Preserve absolute URLs provided by client
                    if (thuocDto.UrlAnh.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || thuocDto.UrlAnh.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        entity.UrlAnh = thuocDto.UrlAnh;
                    }
                    else
                    {
                        var extracted = ExtractFileNameFromUrl(thuocDto.UrlAnh);
                        entity.UrlAnh = extracted ?? thuocDto.UrlAnh;
                    }
                }
            }

            entity.MaLoaiThuoc = thuocDto.MaLoaiThuoc ?? entity.MaLoaiThuoc;
            entity.TenThuoc = thuocDto.TenThuoc ?? entity.TenThuoc;
            entity.ThanhPhan = thuocDto.ThanhPhan ?? entity.ThanhPhan;
            entity.MoTa = thuocDto.MoTa ?? entity.MoTa;
            // Note: unit/pricing moved to GIATHUOC; Thuoc table does not store MaLoaiDonVi/SoLuong
            entity.CongDung = thuocDto.CongDung ?? entity.CongDung;
            entity.CachDung = thuocDto.CachDung ?? entity.CachDung;
            entity.LuuY = thuocDto.LuuY ?? entity.LuuY;
            entity.MaNCC = thuocDto.MaNCC ?? entity.MaNCC;
            entity.Code = thuocDto.Code ?? entity.Code;
            // Pricing lives in GIATHUOC now; update prices via separate endpoints for GIATHUOC

            try
            {
                if ((thuocDto.FileAnh == null || thuocDto.FileAnh.Length == 0) && !string.IsNullOrWhiteSpace(thuocDto.UrlAnh))
                {
                    // If UrlAnh is absolute, preserve it. Otherwise try to extract the filename.
                    if (thuocDto.UrlAnh.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || thuocDto.UrlAnh.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        entity.UrlAnh = thuocDto.UrlAnh;
                    }
                    else
                    {
                        var extracted2 = ExtractFileNameFromUrl(thuocDto.UrlAnh);
                        entity.UrlAnh = extracted2 ?? thuocDto.UrlAnh;
                    }
                }
            }
            catch { }

            await _repo.SaveChangesAsync();

            // Update/create GIATHUOC rows if provided
            var ctx = _repo.Context;
            if (thuocDto.GiaThuocs?.Any() == true)
            {
                // Lấy tất cả GiaThuoc hiện tại của thuốc
                var allGiaThuoc = await ctx.GiaThuocs
                    .Where(x => x.MaThuoc == entity.MaThuoc)
                    .ToListAsync();

                // Tạo danh sách các MaGiaThuoc từ client (chỉ những cái có MaGiaThuoc)
                var inputMaGiaThuoc = thuocDto.GiaThuocs
                    .Where(x => !string.IsNullOrEmpty(x.MaGiaThuoc))
                    .Select(x => x.MaGiaThuoc)
                    .ToHashSet();

                // Xác định các GiaThuoc cần xóa (có trong DB nhưng không còn trong input)
                var giaThuocToDelete = allGiaThuoc
                    .Where(x => !inputMaGiaThuoc.Contains(x.MaGiaThuoc))
                    .ToList();

                // Xóa trực tiếp không kiểm tra liên kết
                if (giaThuocToDelete.Any())
                {
                    ctx.GiaThuocs.RemoveRange(giaThuocToDelete);
                    await ctx.SaveChangesAsync();
                }

                // Cập nhật hoặc thêm mới các GiaThuoc từ input
                foreach (var g in thuocDto.GiaThuocs)
                {
                    if (!string.IsNullOrEmpty(g.MaGiaThuoc))
                    {
                        // Cập nhật theo MaGiaThuoc
                        var existing = allGiaThuoc.FirstOrDefault(x => x.MaGiaThuoc == g.MaGiaThuoc);
                        if (existing != null)
                        {
                            existing.MaLoaiDonVi = g.MaLoaiDonVi;
                            existing.SoLuong = g.SoLuong;
                            existing.DonGia = g.DonGia;
                            existing.TrangThai = g.TrangThai;
                            continue;
                        }
                    }

                    // Thêm mới nếu không có MaGiaThuoc hoặc không tìm thấy
                    var newMa = "GT" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    var newRow = new GiaThuoc
                    {
                        MaGiaThuoc = newMa,
                        MaThuoc = entity.MaThuoc,
                        MaLoaiDonVi = g.MaLoaiDonVi,
                        SoLuong = g.SoLuong,
                        DonGia = g.DonGia,
                        TrangThai = g.TrangThai
                    };
                    await ctx.GiaThuocs.AddAsync(newRow);
                }

                await ctx.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> DeleteThuocAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required");

            var ctx = _repo.Context;
            var entity = await _repo.FindAsync(id);
            if (entity == null) throw new Exception("Không tìm thấy thuốc.");

            // Use a transaction so we don't partially delete GiaThuoc when Thuoc is in use
            await using var tx = await ctx.Database.BeginTransactionAsync();
            try
            {
                // Check references that would prevent deletion
                var usedInTonKho = await ctx.TonKhos.AnyAsync(tk => tk.MaThuoc == id);
                var usedInChiTietPhieuNhap = await ctx.ChiTietPhieuNhaps.AnyAsync(ct => ct.MaThuoc == id);

                // check ChiTietHoaDon via linking to TonKho.MaLo
                var usedInChiTietHoaDon = await (from ct in ctx.ChiTietHoaDons
                                                  join tk in ctx.TonKhos on ct.MaLo equals tk.MaLo
                                                  where tk.MaThuoc == id
                                                  select ct).AnyAsync();

                if (usedInTonKho || usedInChiTietPhieuNhap || usedInChiTietHoaDon)
                {
                    // don't delete anything, return informative error
                    throw new Exception("Thuốc đang được sử dụng và không thể xóa.");
                }

                // safe to delete related GiaThuoc rows first
                var giaRows = await ctx.GiaThuocs.Where(g => g.MaThuoc == id).ToListAsync();
                if (giaRows.Any()) ctx.GiaThuocs.RemoveRange(giaRows);

                // remove Thuoc
                _repo.Remove(entity);

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // GET: api/Thuoc/ByCode/{code}
        // Search thuốc by Code field - returns MaThuoc and TenThuoc
        public Task<object> GetThuocByCodeAsync(string code)
        {
            var ctx = _repo.Context;
            return ctx.Thuoc
                .Where(t => t.Code != null && t.Code.Contains(code))
                .Select(t => new
                {
                    t.MaThuoc,
                    t.TenThuoc,
                    t.Code
                })
                .ToListAsync()
                .ContinueWith(t => (object)t.Result);
        }

        // GET: api/Thuoc/SearchCode/{code}
        // Lightweight search - find single match for exact or partial code
        public Task<object?> GetThuocByCodeExactAsync(string code)
        {
            var ctx = _repo.Context;
            return ctx.Thuoc
                .Where(t => t.Code == code)
                .Select(t => new
                {
                    t.MaThuoc,
                    t.TenThuoc,
                    t.Code
                })
                .FirstOrDefaultAsync()
                .ContinueWith(t => (object?)t.Result);
        }
    }
}

