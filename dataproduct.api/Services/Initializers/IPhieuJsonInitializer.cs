using dataproduct.api.Models;

namespace dataproduct.api.Services.Initializers
{
    public interface IPhieuJsonInitializer
    {
        bool CanHandle(string? maBm);
        Task<List<string>> InitializeAsync(BmPhieu phieu);
    }
}
