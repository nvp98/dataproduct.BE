using System.ComponentModel.DataAnnotations.Schema;

namespace dataproduct.api.DTOs
{

public class SiloWithPhuLieuNmDto
{
    public int SiloId { get; set; }
    public string TenSilo { get; set; } = null!;
    public decimal? TheTich { get; set; }
    public List<int> PhuLieuNMIds { get; set; } = new();
}

public class SiloOptionDto
{
    public int SiloId { get; set; }
    public string TenSilo { get; set; } = null!;
    public decimal? TheTich { get; set; }
    public List<int> PhuLieuNMIds { get; set; } = new();
    public bool IsAuto { get; set; }
}

public class SelectSiloResponse
{
    public bool IsAutoSelected { get; set; }
    public int? SiloId { get; set; }
    public int? PhuLieuNMId { get; set; }
    public List<SiloOptionDto> Options { get; set; } = new();
}

public class SelectPhuLieuNMResponse
{
    public bool IsAutoSelected { get; set; }
    public int? PhuLieuNMId { get; set; }
    public List<int> PhuLieuNMIds { get; set; } = new();
}

// Request DTOs
public class ResolveSiloRequest
{
    public List<int> PhuLieuNMIds { get; set; } = new();
    public DateTime NgaySX { get; set; }
}

public class ResolvePhuLieuWhenChangeSiloRequest
{
    public int SiloId { get; set; }
    public List<int> PhuLieuNMIds { get; set; } = new();
    public DateTime NgaySX { get; set; }
}

public class ValidateBeforeSaveRequest
{
    public int SiloId { get; set; }
    public int PhuLieuNMId { get; set; }
    public DateTime NgaySX { get; set; }
}

// MapSiloPhuLieuNM DTOs
public class MapSiloPhuLieuNMPayload
{
    public int SiloId { get; set; }
    public int PhuLieuNMId { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MapSiloPhuLieuNMResponse
{
    public int Id { get; set; }
    public int SiloId { get; set; }
    public string TenSilo { get; set; } = null!;
    public int PhuLieuNMId { get; set; }
    public string TenPhuLieu { get; set; } = null!;
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool IsActive { get; set; }
}

// DTO để lấy silo theo headerkey
public class GetSiloByHeaderKeyRequest
{
    public int HeaderKeyId { get; set; }
    public DateTime NgaySX { get; set; }
    public int NhaMay { get; set; } // HRC1 = 1, HRC2 = 2
    public string? BieuMau { get; set; } // BOF, LF, RH
}

public class SiloByHeaderKeyDto
{
    public int SiloId { get; set; }
    public string TenSilo { get; set; } = null!;
    public decimal? TheTich { get; set; }
}

}