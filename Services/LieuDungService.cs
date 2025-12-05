using BE_QLTiemThuoc.Model;
using BE_QLTiemThuoc.Repositories;

namespace BE_QLTiemThuoc.Services
{
    public class LieuDungService
    {
        private readonly LieuDungRepository _repo;

        public LieuDungService(LieuDungRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<LieuDung>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<LieuDung?> GetByIdAsync(string maLD)
        {
            return await _repo.GetByIdAsync(maLD);
        }

        public async Task<LieuDung> CreateAsync(LieuDung dto)
        {
            // Generate MaLD
            var last = await _repo.GetAllAsync();
            string lastCode = last.OrderByDescending(l => l.MaLD).FirstOrDefault()?.MaLD ?? "LD000";
            int number = 1;
            try { number = int.Parse(lastCode.Substring(2)) + 1; } catch { number = 1; }
            var newMaLD = "LD" + number.ToString("D3");

            var lieuDung = new LieuDung
            {
                MaLD = newMaLD,
                TenLieuDung = dto.TenLieuDung
            };

            await _repo.AddAsync(lieuDung);
            return lieuDung;
        }

        public async Task<LieuDung?> UpdateAsync(string maLD, LieuDung dto)
        {
            var existing = await _repo.GetByIdAsync(maLD);
            if (existing == null) return null;

            existing.TenLieuDung = dto.TenLieuDung;
            await _repo.UpdateAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteAsync(string maLD)
        {
            var existing = await _repo.GetByIdAsync(maLD);
            if (existing == null) return false;

            await _repo.DeleteAsync(maLD);
            return true;
        }
    }
}