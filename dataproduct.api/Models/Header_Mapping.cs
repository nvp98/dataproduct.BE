using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class Header_Mapping
{
    public int Id { get; set; }
   
    public string TenNguonDuLieu { get; set; }
    public int ID_PhuLieu { get; set; }
    public int ID_HeaderKey { get; set; }
    public bool IsActive { get; set; }
    public DateTime? NgayTao { get; set; }
}


