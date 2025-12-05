using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Ban;
using Microsoft.EntityFrameworkCore;

namespace BE_QLTiemThuoc.Repositories
{
 public class BinhLuanRepository
 {
 private readonly AppDbContext _ctx;
 public BinhLuanRepository(AppDbContext ctx){ _ctx = ctx; }

 public Task<BinhLuan?> GetByIdAsync(string id) => _ctx.BinhLuans.FirstOrDefaultAsync(b=>b.MaBL==id);
 public async Task AddAsync(BinhLuan e){ await _ctx.BinhLuans.AddAsync(e); }
 public void Remove(BinhLuan e){ _ctx.BinhLuans.Remove(e); }
 public Task SaveAsync() => _ctx.SaveChangesAsync();
 public Task<List<BinhLuan>> GetAllByThuocAsync(string maThuoc) => _ctx.BinhLuans.Where(b=>b.MaThuoc==maThuoc).OrderByDescending(b=>b.ThoiGian).ToListAsync();
 public Task<List<BinhLuan>> GetRootByThuocAsync(string maThuoc) => _ctx.BinhLuans.Where(b=>b.MaThuoc==maThuoc && b.TraLoiChoBinhLuan==null).OrderByDescending(b=>b.ThoiGian).ToListAsync();

 // New: global access
 public Task<List<BinhLuan>> GetAllAsync() => _ctx.BinhLuans.OrderByDescending(b=>b.ThoiGian).ToListAsync();
 public Task<List<BinhLuan>> GetAllRootsAsync() => _ctx.BinhLuans.Where(b=>b.TraLoiChoBinhLuan==null).OrderByDescending(b=>b.ThoiGian).ToListAsync();

 // Unanswered comments for a product (legacy): now just return flat ordered list; service computes unanswered
 public async Task<List<BinhLuan>> GetUnansweredByThuocAsync(string maThuoc)
 {
 var flat = await _ctx.BinhLuans.Where(b=>b.MaThuoc==maThuoc).ToListAsync();
 var byId = flat.ToDictionary(b=>b.MaBL);
 string GetRootId(BinhLuan b)
 {
 while(b.TraLoiChoBinhLuan!=null && byId.TryGetValue(b.TraLoiChoBinhLuan, out var parent)) b = parent;
 return b.MaBL;
 }
 return flat
 .OrderBy(b=>GetRootId(b))
 .ThenBy(b=>b.TraLoiChoBinhLuan==null?0:1)
 .ThenBy(b=>b.ThoiGian)
 .ToList();
 }
 }
}
