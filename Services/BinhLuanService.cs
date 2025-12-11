using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Model.Ban;
using BE_QLTiemThuoc.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace BE_QLTiemThuoc.Services
{
 public class BinhLuanService
 {
 private readonly BinhLuanRepository _repo;
 public BinhLuanService(BinhLuanRepository repo){ _repo = repo; }

 // Helper method để lấy dictionary tên KH và NV từ danh sách bình luận
 private async Task<(Dictionary<string, string> khNames, Dictionary<string, string> nvNames)> GetNamesAsync(List<BinhLuan> flat)
 {
     var ctx = _repo.Context;
     var maKHs = flat.Where(b => b.MaKH != null).Select(b => b.MaKH!).Distinct().ToList();
     var maNVs = flat.Where(b => b.MaNV != null).Select(b => b.MaNV!).Distinct().ToList();
     
     var khNames = await ctx.KhachHangs
         .Where(k => maKHs.Contains(k.MAKH))
         .ToDictionaryAsync(k => k.MAKH, k => k.HoTen ?? k.MAKH);
     
     var nvNames = await ctx.Set<Model.NhanVien>()
         .Where(n => maNVs.Contains(n.MaNV))
         .ToDictionaryAsync(n => n.MaNV, n => n.HoTen ?? n.MaNV);
     
     return (khNames, nvNames);
 }

 public async Task<BinhLuanViewDto?> GetAsync(string maBL)
 {
 var target = await _repo.GetByIdAsync(maBL);
 if(target==null) return null;
 var flat = await _repo.GetAllByThuocAsync(target.MaThuoc); // load all for product
 var (khNames, nvNames) = await GetNamesAsync(flat);
 return BuildSubTree(flat, target.MaBL, khNames, nvNames);
 }

 public async Task<List<BinhLuanViewDto>> GetByThuocAsync(string maThuoc)
 {
 var flat = await _repo.GetAllByThuocAsync(maThuoc);
 var (khNames, nvNames) = await GetNamesAsync(flat);
 return BuildForest(flat, khNames, nvNames);
 }

 public async Task<List<BinhLuanViewDto>> GetRootByThuocAsync(string maThuoc)
 {
 var roots = await _repo.GetRootByThuocAsync(maThuoc);
 var flat = await _repo.GetAllByThuocAsync(maThuoc);
 var (khNames, nvNames) = await GetNamesAsync(flat);
 var map = BuildChildrenMap(flat);
 return roots.OrderByDescending(r=>r.ThoiGian).Select(r=>ToDtoRecursive(r, map, khNames, nvNames)).ToList();
 }

 // Admin: unanswered comments for product, ordered root->[child]
 public async Task<List<BinhLuanViewDto>> GetUnansweredByThuocAsync(string maThuoc)
 {
 var list = await _repo.GetUnansweredByThuocAsync(maThuoc);
 // build minimal nested context: for items returned, attach their direct children if any unanswered
 var flat = await _repo.GetAllByThuocAsync(maThuoc);
 var (khNames, nvNames) = await GetNamesAsync(flat);
 var map = BuildChildrenMap(flat);
 return list.Select(b=>ToDtoRecursive(b, map, khNames, nvNames)).ToList();
 }

 public async Task<BinhLuanViewDto> CreateAsync(BinhLuanCreateDto dto)
 {
 if(string.IsNullOrWhiteSpace(dto.MaThuoc)) throw new Exception("MaThuoc required");
 if(string.IsNullOrWhiteSpace(dto.NoiDung)) throw new Exception("NoiDung required");
 var hasAuthor = !string.IsNullOrWhiteSpace(dto.MaKH) ^ !string.IsNullOrWhiteSpace(dto.MaNV);
 if(!hasAuthor) throw new Exception("Provide MaKH or MaNV (exactly one)");

 if(!string.IsNullOrWhiteSpace(dto.TraLoiChoBinhLuan))
 {
 var parent = await _repo.GetByIdAsync(dto.TraLoiChoBinhLuan) ?? throw new Exception("Parent comment not found");
 if(parent.MaThuoc != dto.MaThuoc) throw new Exception("Parent belongs to different product");
 var flat = await _repo.GetAllByThuocAsync(dto.MaThuoc);
 if(flat.Any(b=>b.TraLoiChoBinhLuan == dto.TraLoiChoBinhLuan)) throw new Exception("B�nh lu?n n�y ?� ???c tr? l?i. Kh�ng th? tr? l?i th�m.");
 }

 var e = new BinhLuan{
 MaBL = Guid.NewGuid().ToString("N").Substring(0,20),
 MaThuoc = dto.MaThuoc,
 MaKH = dto.MaKH,
 MaNV = dto.MaNV,
 NoiDung = dto.NoiDung,
 TraLoiChoBinhLuan = dto.TraLoiChoBinhLuan,
 ThoiGian = DateTime.UtcNow,
 // ignore DaTraLoi persistence; derive status dynamically
 };
 await _repo.AddAsync(e);
 try
 {
 await _repo.SaveAsync();
 }
 catch (DbUpdateException ex)
 {
 if(ex.InnerException is SqlException sql)
 {
 if(sql.Number==2601 || sql.Number==2627 || sql.Message.Contains("Bình luận này đã được trả lời"))
 throw new Exception("Bình luận này đã được trả lời. Không thể trả lời thêm.");
 }
 throw;
 }
 var (khNames, nvNames) = await GetNamesAsync(new List<BinhLuan> { e });
 return ToDto(e, khNames, nvNames);
 }

 public async Task<BinhLuanViewDto?> UpdateAsync(string maBL, BinhLuanUpdateDto dto)
 {
 if(string.IsNullOrWhiteSpace(dto.NoiDung)) throw new Exception("NoiDung required");
 
 var e = await _repo.GetByIdAsync(maBL);
 if(e == null) return null;
 
 e.NoiDung = dto.NoiDung;
 await _repo.SaveAsync();
 
 var (khNames, nvNames) = await GetNamesAsync(new List<BinhLuan> { e });
 return ToDto(e, khNames, nvNames);
 }

 public async Task<bool> DeleteAsync(string maBL)
 {
 var e = await _repo.GetByIdAsync(maBL);
 if(e==null) return false;
 
 // Lấy tất cả bình luận của thuốc để tìm các bình luận con
 var allComments = await _repo.GetAllByThuocAsync(e.MaThuoc);
 
 // Hàm đệ quy để lấy tất cả các bình luận con (replies) của một bình luận
 List<BinhLuan> GetAllReplies(string parentId)
 {
     var directReplies = allComments.Where(b => b.TraLoiChoBinhLuan == parentId).ToList();
     var allReplies = new List<BinhLuan>();
     foreach(var reply in directReplies)
     {
         allReplies.AddRange(GetAllReplies(reply.MaBL)); // Lấy replies của reply
         allReplies.Add(reply);
     }
     return allReplies;
 }
 
 // Lấy tất cả các bình luận con (bao gồm cả con của con)
 var replies = GetAllReplies(maBL);
 
 // Xóa các bình luận con trước (từ sâu nhất đến gần nhất)
 foreach(var reply in replies)
 {
     _repo.Remove(reply);
 }
 
 // Xóa bình luận chính
 _repo.Remove(e);
 await _repo.SaveAsync();
 return true;
 }

 private static List<BinhLuanViewDto> BuildForest(List<BinhLuan> flat, Dictionary<string, string> khNames, Dictionary<string, string> nvNames)
 {
 var childrenMap = BuildChildrenMap(flat);
 return flat
 .Where(b=>b.TraLoiChoBinhLuan==null)
 .OrderByDescending(b=>b.ThoiGian)
 .Select(root=>ToDtoRecursive(root, childrenMap, khNames, nvNames))
 .ToList();
 }

 private static BinhLuanViewDto BuildSubTree(List<BinhLuan> flat, string rootId, Dictionary<string, string> khNames, Dictionary<string, string> nvNames)
 {
 var childrenMap = BuildChildrenMap(flat);
 var root = flat.First(b=>b.MaBL==rootId);
 return ToDtoRecursive(root, childrenMap, khNames, nvNames);
 }

 private static Dictionary<string,List<BinhLuan>> BuildChildrenMap(List<BinhLuan> flat)
 {
 var dict = new Dictionary<string,List<BinhLuan>>();
 foreach(var bl in flat)
 {
 if(bl.TraLoiChoBinhLuan!=null)
 {
 if(!dict.TryGetValue(bl.TraLoiChoBinhLuan, out var list))
 {
 list = new List<BinhLuan>();
 dict[bl.TraLoiChoBinhLuan] = list;
 }
 list.Add(bl);
 }
 }
 return dict;
 }

 private static BinhLuanViewDto ToDtoRecursive(BinhLuan e, Dictionary<string,List<BinhLuan>> childrenMap, Dictionary<string, string> khNames, Dictionary<string, string> nvNames)
 {
 var dto = ToDto(e, khNames, nvNames);
 if(childrenMap.TryGetValue(e.MaBL, out var kids))
 dto.Replies = kids.OrderByDescending(c=>c.ThoiGian).Select(c=>ToDtoRecursive(c, childrenMap, khNames, nvNames)).ToList();
 return dto;
 }

 private static BinhLuanViewDto ToDto(BinhLuan e, Dictionary<string, string> khNames, Dictionary<string, string> nvNames) => new(){
 MaBL = e.MaBL,
 MaThuoc = e.MaThuoc,
 MaKH = e.MaKH,
 TenKH = e.MaKH != null && khNames.TryGetValue(e.MaKH, out var tenKH) ? tenKH : null,
 MaNV = e.MaNV,
 TenNV = e.MaNV != null && nvNames.TryGetValue(e.MaNV, out var tenNV) ? tenNV : null,
 NoiDung = e.NoiDung,
 ThoiGian = e.ThoiGian,
 TraLoiChoBinhLuan = e.TraLoiChoBinhLuan,
 Replies = new List<BinhLuanViewDto>()
 };

 // Admin: global list of roots with status (0 = chưa trả lời,1 = đã trả lời) considering subtree
 public async Task<List<AdminRootStatusDto>> GetGlobalRootStatusAsync()
 {
 var flat = await _repo.GetAllAsync();
 var (khNames, nvNames) = await GetNamesAsync(flat);
 var childrenMap = BuildChildrenMap(flat);
 BinhLuan GetDeepest(BinhLuan b)
 {
 while(childrenMap.TryGetValue(b.MaBL, out var kids) && kids.Count>0)
 {
 // single direct reply per design; pick the only child
 b = kids.OrderByDescending(x=>x.ThoiGian).First();
 }
 return b;
 }
 var roots = flat.Where(b=>b.TraLoiChoBinhLuan==null).OrderByDescending(b=>b.ThoiGian);
 var result = new List<AdminRootStatusDto>();
 foreach(var r in roots)
 {
 var dto = ToDtoRecursive(r, childrenMap, khNames, nvNames);
 var deepest = GetDeepest(r);
 var status = string.IsNullOrWhiteSpace(deepest.MaNV) ?0 :1; //0: last not staff => chưa trả lời
 result.Add(new AdminRootStatusDto{ Root = dto, Status = status });
 }
 return result;
 }
 }
}
