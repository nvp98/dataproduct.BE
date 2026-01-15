namespace dataproduct.api.DTOs.Export
{
    public class ExportFileResult
    {
        public byte[] Content { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
    }
}
