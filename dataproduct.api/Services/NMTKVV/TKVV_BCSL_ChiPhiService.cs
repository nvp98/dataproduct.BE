using dataproduct.api.DTOs.NMTKVV_Dto;
using dataproduct.api.Repositories.NMTKVV;

namespace dataproduct.api.Services.NMTKVV
{
    public class TKVV_BCSL_ChiPhiService
    {
        private readonly ITKVV_BCSL_ChiPhiRepository _repo;

        public TKVV_BCSL_ChiPhiService(ITKVV_BCSL_ChiPhiRepository repo)
        {
            _repo = repo;
        }

        // scope int 1-6 → map sang code "TK1"/"VV2"... trước khi gọi SP
        public Task<List<TKVVGiaTriNVLAutoDto>> GetGiaTriNVLAutoAsync(
            DateTime ngay, int ca, int scope, string maBM)
        {
            var scopeCode = TKVV_BCSL_ChiPhiRepository.ResolveScopeCode(scope);
            return _repo.GetGiaTriNVLAutoAsync(ngay, ca, scopeCode, maBM);
        }
    }
}
