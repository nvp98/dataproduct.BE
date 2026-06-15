using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Models.MasterData;

[Keyless]
public class TonSiLoLoCaResult
{
    public int? IDSilo { get; set; }
    public int? IDCa { get; set; }
    public DateTime? Time { get; set; }
    public decimal? TonUocTinh { get; set; }
}
