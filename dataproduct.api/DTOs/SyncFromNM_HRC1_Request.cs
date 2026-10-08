using System;

namespace dataproduct.api.DTOs
{
    public class SyncFromNM_HRC1_Request
    {
        public DateTime NgaySX { get; set; }
        public int Ca { get; set; }
        public int Scope { get; set; }
        public string BieuMau { get; set; } = "BOF";
    }
}
