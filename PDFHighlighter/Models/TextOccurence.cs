namespace PDFHighlighter.Models
{
    public class TextOccurence
    {
        private List<ContentDetail>? contentDetails;

        public string? SearchText { get; set; }
        public decimal[]? AreaBoundingBox { get; set; }
        public List<ContentDetail>? ContentDetails { get => contentDetails; set => contentDetails = value; }
    }
}
