namespace PDFHighlighter.Models
{
    public class ContentDetail
    {
        public decimal? PageWidth {  get; set; }
        public decimal? PageHeight { get; set; }
        public decimal? PageNumber { get; set; }
        public string? SearchText { get; set; }
        public decimal[]? Polygon { get; set; }
        public Span? Span { get; set; } = new Span();
        public decimal? Confidence { get; set; }
    }
}
