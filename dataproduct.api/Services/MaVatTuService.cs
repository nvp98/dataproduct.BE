using dataproduct.api.DTOs;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class MaVatTuService
    {
        private readonly IMaVatTuRepository _repo;

        public MaVatTuService(IMaVatTuRepository repo)
        {
            _repo = repo;
        }

        public Task<(IEnumerable<MaVatTuItem> Data, int TotalCount)> SearchAsync(MaVatTuSearchRequest req)
            => _repo.SearchAsync(req);

        public Task<MaVatTuItem?> GetByIdAsync(int id)
            => _repo.GetByIdAsync(id);

        public Task<MaVatTuItem> CreateAsync(MaVatTuUpsertDto dto)
            => _repo.CreateAsync(dto);

        public Task<MaVatTuBulkCreateResult> BulkCreateAsync(List<MaVatTuUpsertDto> items)
            => _repo.BulkCreateAsync(items);

        public Task<bool> UpdateAsync(int id, MaVatTuUpsertDto dto)
            => _repo.UpdateAsync(id, dto);

        public Task<bool> DeleteAsync(int id)
            => _repo.DeleteAsync(id);

        public Task<Dictionary<string, string>> GetMaVatTuMapAsync(string nhaMay, IEnumerable<string> macThepNames)
            => _repo.GetMaVatTuMapAsync(nhaMay, macThepNames);
    }
}
