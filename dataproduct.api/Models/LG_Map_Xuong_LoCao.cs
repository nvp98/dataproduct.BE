using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_Map_Xuong_LoCao
    {
        [Key]
        public int ID_Xuong { get; set; }
        public int IDLoCao { get; set; }
    }
}
