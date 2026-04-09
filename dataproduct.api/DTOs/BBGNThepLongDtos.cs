using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.DTOs
{
    public class FetchMeThoiRequest
    {
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int NhaMay { get; set; }
    }

    public class LoadBBGNThepLongRequest
    {
        public Guid IdPhieu { get; set; }
        public DateOnly NgaySX { get; set; }
        public int Ca { get; set; }
        public int NhaMay { get; set; }
    }

    public enum NhaMay
    {
        HRC1 = 1,
        HRC2 = 2
    }
}

