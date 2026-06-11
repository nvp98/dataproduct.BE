using dataproduct.api.ResponseModels;

namespace dataproduct.api.Services.PhieuEnrichers;

public interface IPhieuSearchEnricher
{
    string MaBm { get; }
    Task EnrichAsync(SearchPhieuResponseModel item);
}
