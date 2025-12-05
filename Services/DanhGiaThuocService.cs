using BE_QLTiemThuoc.Dto;
using BE_QLTiemThuoc.Model.Ban;
using BE_QLTiemThuoc.Repositories;

namespace BE_QLTiemThuoc.Services
{
 public class DanhGiaThuocService
 {
 private readonly DanhGiaThuocRepository _repo;
 public DanhGiaThuocService(DanhGiaThuocRepository repo)
 {
 _repo = repo;
 }

 public async Task<DanhGiaThuocViewDto?> GetAsync(string maDanhGia)
 {
 var e = await _repo.GetByIdAsync(maDanhGia);
 return e==null? null : ToViewDto(e);
 }

 public async Task<List<DanhGiaThuocViewDto>> GetByThuocAsync(string maThuoc)
 {
 var list = await _repo.GetByThuocAsync(maThuoc);
 return list.Select(ToViewDto).ToList();
 }

 public async Task<List<DanhGiaThuocViewDto>> GetByKhachHangAsync(string maKh)
 {
 var list = await _repo.GetByKhachHangAsync(maKh);
 return list.Select(ToViewDto).ToList();
 }

 public async Task<List<DanhGiaThuocEligibleDto>> GetEligibleThuocAsync(string maKh)
 {
 var eligible = await _repo.GetEligibleThuocByKhachAsync(maKh);
 var existingRatings = await _repo.GetByKhachHangAsync(maKh);
 var dict = existingRatings.ToDictionary(r => r.MaThuoc, r => r);
 var result = new List<DanhGiaThuocEligibleDto>();
 foreach(var item in eligible)
 {
 if(dict.TryGetValue(item.MaThuoc, out var r))
 {
 result.Add(new DanhGiaThuocEligibleDto
 {
 MaThuoc = item.MaThuoc,
 TenThuoc = item.TenThuoc ?? string.Empty,
 DaDanhGia = true,
 MaDanhGia = r.MaDanhGia,
 SoSao = r.SoSao
 });
 }
 else
 {
 result.Add(new DanhGiaThuocEligibleDto
 {
 MaThuoc = item.MaThuoc,
 TenThuoc = item.TenThuoc ?? string.Empty,
 DaDanhGia = false
 });
 }
 }
 return result.OrderByDescending(x => x.DaDanhGia).ThenBy(x=>x.TenThuoc).ToList();
 }

 public async Task<DanhGiaThuocViewDto> CreateOrUpdateAsync(DanhGiaThuocCreateDto dto)
 {
 // Validate completed order
 if(!await _repo.HasCompletedOrderForThuocAsync(dto.MaKH, dto.MaThuoc))
 throw new Exception("Khách ch?a mua ho?c ??n hàng ch?a hoàn thành cho s?n ph?m này (tr?ng thái giao hàng ph?i =3).");

 var existing = await _repo.GetByKhachHangAndThuocAsync(dto.MaKH, dto.MaThuoc);
 if(existing==null)
 {
 existing = new DanhGiaThuoc
 {
 MaDanhGia = Guid.NewGuid().ToString("N").Substring(0,20),
 MaKH = dto.MaKH,
 MaThuoc = dto.MaThuoc,
 SoSao = dto.SoSao,
 NoiDung = dto.NoiDung,
 NgayDanhGia = DateTime.UtcNow
 };
 await _repo.AddAsync(existing);
 }
 else
 {
 existing.SoSao = dto.SoSao;
 existing.NoiDung = dto.NoiDung;
 existing.NgayDanhGia = DateTime.UtcNow;
 }
 await _repo.SaveChangesAsync();
 return ToViewDto(existing);
 }

 public async Task<DanhGiaThuocViewDto> UpdateAsync(string maDanhGia, DanhGiaThuocUpdateDto dto)
 {
 var e = await _repo.GetByIdAsync(maDanhGia) ?? throw new Exception("Không tìm th?y ?ánh giá");
 e.SoSao = dto.SoSao;
 e.NoiDung = dto.NoiDung;
 e.NgayDanhGia = DateTime.UtcNow;
 await _repo.SaveChangesAsync();
 return ToViewDto(e);
 }

 public async Task<bool> DeleteAsync(string maDanhGia)
 {
 var e = await _repo.GetByIdAsync(maDanhGia);
 if(e==null) return false;
 _repo.Remove(e);
 await _repo.SaveChangesAsync();
 return true;
 }

 private static DanhGiaThuocViewDto ToViewDto(DanhGiaThuoc e) => new()
 {
 MaDanhGia = e.MaDanhGia,
 MaKH = e.MaKH,
 MaThuoc = e.MaThuoc,
 SoSao = e.SoSao,
 NoiDung = e.NoiDung,
 NgayDanhGia = e.NgayDanhGia
 };
 }
}
