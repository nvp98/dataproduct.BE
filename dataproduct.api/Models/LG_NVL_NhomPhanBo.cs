using System.ComponentModel.DataAnnotations;

namespace dataproduct.api.Models
{
    public class LG_NVL_NhomPhanBo
    {
        [Key]
        public int ID { get; set; }
        public int IDNVL { get; set; }
        public int IDNhomPhanBo { get; set; }
        public int ThuTuUuTien { get; set; }
        public bool IsDelete { get; set; }
    }
}
