namespace PDFHighlighter.Models
{
    public class Span
    {
        public decimal? Offset { get; set; }
        public decimal? Length { get; set; }
    }

    public class JsonProcessRequest
    {
        public string? Json { get; set; }
        public string? SearchText { get; set; }
    }
}
