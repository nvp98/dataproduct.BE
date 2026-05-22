using System;
using System.Collections.Generic;

namespace dataproduct.api.Models;

public partial class BmQuyenXl
{
    public int Id { get; set; }

    public int? IdTaiKhoan { get; set; }

    public string? MaBm { get; set; }

    public string? MaKhuVuc { get; set; }

    // tinyint trong database => byte? trong model
    public byte? QuyenChucNang { get; set; }
    public string? KhuVucPhu {get;set;}
}
