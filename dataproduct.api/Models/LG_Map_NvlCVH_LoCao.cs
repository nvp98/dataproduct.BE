using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    // NVL "than cốc hoàn" đại diện cho khối lượng nhận về (G) của CVH tại mỗi lò cao
    public class LG_Map_NvlCVH_LoCao
    {
        [Key]
        public int IDLoCao { get; set; }
        public int IDNVL { get; set; }
    }
}
