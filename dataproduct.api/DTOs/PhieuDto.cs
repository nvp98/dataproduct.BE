using dataproduct.api.Models;
using System.Text.Json;

namespace dataproduct.api.DTOs
{
    public class PhieuDto
    {
        public Guid Idphieu { get; set; }

        public string MaBm { get; set; } = null!;

        public string? SoPhieu { get; set; }

        public int? XuongId { get; set; }

        public int? IdphongBan { get; set; }

        public int? Idkip { get; set; }

        public int? Ca { get; set; }

        public string? Kip { get; set; }

        public DateTime? NgayTao { get; set; }
        public DateOnly? NgaySX { get; set; }

        public int? MayDuc { get; set; }

        public int? NguoiTaoId { get; set; }

        public int? TinhTrang { get; set; }

        public string? DataJson { get; set; }

        public int? IsDelete { get; set; }

        public int? IsLock { get; set; }
        public int? LoaiPhieu { get; set; }
        public bool? IsClone { get; set; }
        public int? VersionClone { get; set; }
        public Guid? ID_PhieuGoc { get; set; }
        public JsonElement? JsonData { get; internal set; }
        public List<BM_PheDuyetDto>? PheDuyet { get; set; }
    }
}
