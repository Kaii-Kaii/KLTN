using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BE_QLTiemThuoc.Data;
using BE_QLTiemThuoc.Model.Chat;
using BE_QLTiemThuoc.Dto;

namespace BE_QLTiemThuoc.Repositories
{
 public class ChatRepository
 {
 private readonly AppDbContext _ctx;
 public ChatRepository(AppDbContext ctx){ _ctx = ctx; }

 public async Task<CuocTroChuyen> CreateConversationAsync(string maKH)
 {
 var c = new CuocTroChuyen{ MaKH = maKH };
 _ctx.Set<CuocTroChuyen>().Add(c);
 await _ctx.SaveChangesAsync();
 return c;
 }

 public async Task<CuocTroChuyen?> GetLatestConversationByKhAsync(string maKH)
 {
 return await _ctx.CuocTroChuyens
 .Where(c => c.MaKH == maKH)
 .OrderByDescending(c => c.MaCuocTroChuyen)
 .FirstOrDefaultAsync();
 }

 public async Task<CuocTroChuyen> GetOrCreateConversationAsync(string maKH)
 {
 var existing = await GetLatestConversationByKhAsync(maKH);
 if (existing != null) return existing;
 return await CreateConversationAsync(maKH);
 }

 public async Task<ConversationWithMessagesDto?> GetConversationWithMessagesAsync(string maKH, int take)
 {
    var convo = await GetOrCreateConversationAsync(maKH);
var msgs = await _ctx.TinNhans
 .Where(m => m.MaCuocTroChuyen == convo.MaCuocTroChuyen)
 .OrderByDescending(m => m.ThoiGian)
 .Take(take)
 .Select(m => new ChatMessageDto
 {
 MaTN = m.MaTN,
 LaKhachGui = m.LaKhachGui,
 MaNV = m.MaNV,
 NoiDung = m.NoiDung,
 ThoiGian = m.ThoiGian
 })
 .ToListAsync();
 var ten = await _ctx.KhachHangs.Where(k => k.MAKH == convo.MaKH).Select(k => k.HoTen).FirstOrDefaultAsync();
 return new ConversationWithMessagesDto
 {
 MaCuocTroChuyen = convo.MaCuocTroChuyen,
 MaKH = convo.MaKH,
 TenKH = ten,
 Messages = msgs
 };
 }

 public async Task<TinNhan> AddMessageAsync(TinNhan m)
 {
 _ctx.Set<TinNhan>().Add(m);
 await _ctx.SaveChangesAsync();
 return m;
 }

 public async Task<List<TinNhan>> GetMessagesAsync(long maCuoc, int skip, int take)
 {
 return await _ctx.Set<TinNhan>()
 .Where(x=> x.MaCuocTroChuyen==maCuoc)
 .OrderByDescending(x=> x.ThoiGian)
 .Skip(skip)
 .Take(take)
 .ToListAsync();
 }

 public async Task<bool> ConversationExistsAsync(long id)
 => await _ctx.Set<CuocTroChuyen>().AnyAsync(x=> x.MaCuocTroChuyen==id);

 public async Task<List<ConversationSummaryDto>> GetConversationSummariesAsync(int skip, int take, bool? onlyUnanswered)
 {
 var q = _ctx.CuocTroChuyens
 .Select(c => new
 {
 c.MaCuocTroChuyen,
 c.MaKH,
 TenKH = _ctx.KhachHangs.Where(k => k.MAKH == c.MaKH).Select(k => k.HoTen).FirstOrDefault(),
 LastNoiDung = _ctx.TinNhans.Where(t => t.MaCuocTroChuyen == c.MaCuocTroChuyen)
 .OrderByDescending(t => t.ThoiGian).Select(t => t.NoiDung).FirstOrDefault(),
 LastThoiGian = _ctx.TinNhans.Where(t => t.MaCuocTroChuyen == c.MaCuocTroChuyen)
 .OrderByDescending(t => t.ThoiGian).Select(t => (DateTime?)t.ThoiGian).FirstOrDefault(),
 LastLaKhachGui = _ctx.TinNhans.Where(t => t.MaCuocTroChuyen == c.MaCuocTroChuyen)
 .OrderByDescending(t => t.ThoiGian).Select(t => (bool?)t.LaKhachGui).FirstOrDefault(),
 TongTinNhan = _ctx.TinNhans.Count(t => t.MaCuocTroChuyen == c.MaCuocTroChuyen)
 });

 if (onlyUnanswered == true)
 q = q.Where(x => x.LastLaKhachGui == true);

 var list = await q
 .OrderByDescending(x => x.LastThoiGian)
 .ThenByDescending(x => x.MaCuocTroChuyen)
 .Skip(skip)
 .Take(take)
 .ToListAsync();

 return list.Select(x => new ConversationSummaryDto
 {
 MaCuocTroChuyen = x.MaCuocTroChuyen,
 MaKH = x.MaKH,
 TenKH = x.TenKH,
 LastNoiDung = x.LastNoiDung,
 LastThoiGian = x.LastThoiGian,
 LastLaKhachGui = x.LastLaKhachGui,
 ChuaTraLoi = x.LastLaKhachGui == true,
 TongTinNhan = x.TongTinNhan
 }).ToList();
 }
 }
}
