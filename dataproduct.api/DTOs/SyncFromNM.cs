using dataproduct.api.Models;
using System.Text.Json;

namespace dataproduct.api.DTOs
{
    public class SyncFromNM_HRC2_Request
    {
        public int Ca { get; set; }
        public string LoaiBM { get; set; }
        public DateTime NgaySX { get; set; }
        public int Scope { get; set; }
        
    }
}
