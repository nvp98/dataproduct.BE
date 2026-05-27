using System;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.ResponseModels
{
    /// <summary>
    /// Model hứng dữ liệu tổng hợp phụ liệu theo ngày/ca từ stored procedure.
    /// </summary>
    //[Keyless]
    //public class STD_NXT_Filter
    //{
    //    public string? BieuMau { get; set; }
    //    public decimal Scope { get; set; }
    //    public decimal ID_PhuLieu { get; set; }
    //    public string? TenPhuLieu { get; set; }
    //    public double? TotalKLPhuGia { get; set; }
    //}
    [Keyless]
    public class STD_NXT_Filter
    {
        public string? BieuMau { get; set; }
        public int? Scope { get; set; }
        public int? ID_HeaderKey { get; set; }
        public int? ID_PhuLieu { get; set; }
        public string? TenPhuLieu { get; set; }
        public double? TotalKLPhuGia { get; set; }
    }
    [Keyless]
    public class STD_NXT_Filter_Init
    {
        public int Id_HeaderKey { get; set; }
        public string TenNguyenLieu { get; set; }
        public decimal? TonDauCa { get; set; }
        public decimal? NhapVaoTrongCa { get; set; }
        public decimal? TonCuoiCa { get; set; }
        public int Scope { get; set; }
    }


}

