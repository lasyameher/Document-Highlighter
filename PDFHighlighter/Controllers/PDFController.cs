using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PDFHighlighter.Models;

namespace PDFHighlighter.Controllers
{
    public class PdfController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("pdf/upload")]
        public IActionResult Upload(IFormFile pdfFile, IFormFile jsonFile, string searchText)
        {
            if (pdfFile == null || jsonFile == null)
                return BadRequest("Please upload both PDF and JSON");

            var uploadDir = Path.Combine("wwwroot", "uploads");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var pdfPath = Path.Combine(uploadDir, pdfFile.FileName);
            var jsonPath = Path.Combine(uploadDir, jsonFile.FileName);

            using (var fs = new FileStream(pdfPath, FileMode.Create))
                pdfFile.CopyTo(fs);

            using (var fs = new FileStream(jsonPath, FileMode.Create))
                jsonFile.CopyTo(fs);

            string jsonContent = System.IO.File.ReadAllText(jsonPath);

            return Json(new
            {
                pdfUrl = "/uploads/" + pdfFile.FileName,
                json = jsonContent,
                searchString = searchText
            });
        }

        [HttpPost("pdf/process-json")]
        public IActionResult ProcessJson([FromBody] JsonProcessRequest request)
        {
            if (string.IsNullOrEmpty(request.Json) || string.IsNullOrEmpty(request.SearchText))
                return BadRequest("Invalid input.");

            dynamic? doc = JsonConvert.DeserializeObject<dynamic>(request.Json);
            var pages = doc?.pages;
            if (pages == null)
                return Ok(new List<object>());

            // ---------------------------------------------------------
            // Normalize search tokens
            // ---------------------------------------------------------
            string searchLower = request.SearchText.ToLower();
            char[] separator = new char[] {' ', '\n', '\r', '\t' };
            var tokens = searchLower
                .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => new string(t.Where(char.IsLetterOrDigit).ToArray()))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            bool isSingleWord = tokens.Count == 1;
            string? singleWord = isSingleWord ? tokens[0] : null;

            var contentDetails = new List<ContentDetail>();

            foreach (var page in pages)
            {
                int pageNumber = (int)page.pageNumber;
                decimal pageWidth = page.width;
                decimal pageHeight = page.height;

                var words = page.words.ToObject<List<dynamic>>();

                // ---------------------------------------------------------
                // SINGLE WORD MODE
                // ---------------------------------------------------------
                if (isSingleWord)
                {
                    foreach (var w in words)
                    {
                        string cleaned = TextCleaner((string)w.content);

                        if (cleaned == singleWord)
                        {
                            decimal[] poly = w.polygon.ToObject<decimal[]>();
                            contentDetails.Add(new ContentDetail
                            {
                                PageWidth = pageWidth,
                                PageHeight = pageHeight,
                                PageNumber = pageNumber,
                                Polygon = poly,
                                SearchText = request.SearchText
                            });
                        }
                    }
                }

                // ---------------------------------------------------------
                // MULTI WORD: Sequential ordered matching
                // ---------------------------------------------------------
                else
                {
                    for (int i = 0; i < words.Count; i++)
                    {
                        int k = 0;
                        List<decimal[]> tmpPolygons = new List<decimal[]>();
                        // Attempt match starting at word i
                        for (int j = i; j < words.Count && k < tokens.Count; j++)
                        {
                            string cleaned = TextCleaner((string)words[j].content);

                            if (cleaned == tokens[k])
                            {
                                tmpPolygons.Add(words[j].polygon.ToObject<decimal[]>());
                                k++;
                            }
                            else
                            {
                                // mismatch → this starting point invalid
                                break;
                            }
                        }

                        // Full match found
                        if (k == tokens.Count)
                        {
                            decimal[] merged = MergeBoundingBoxes(tmpPolygons);

                            contentDetails.Add(new ContentDetail
                            {
                                PageWidth = pageWidth,
                                PageHeight = pageHeight,
                                PageNumber = pageNumber,
                                Polygon = new decimal[] {
                                    merged[0],
                                    merged[1],
                                    merged[2],
                                    merged[3]
                                },
                                SearchText = request.SearchText
                            });

                            return Ok(contentDetails);
                        }
                    }
                }
            }
            if (contentDetails.Count == 0)
            {
                return Ok("word not found");
            }
            return Ok(contentDetails);
        }

        public static string TextCleaner(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Lowercase + remove anything that's not alphanumeric
            return new string(
                input
                    .ToLower()
                    .Where(char.IsLetterOrDigit)
                    .ToArray()
            );
        }

        // Merge polygons (PDF space)
        private static decimal[] MergeBoundingBoxes(List<decimal[]> polys)
        {
            decimal minX = polys.Min(p => new decimal[] { p[0], p[2], p[4], p[6] }.Min());
            decimal maxX = polys.Max(p => new decimal[] { p[0], p[2], p[4], p[6] }.Max());
            decimal minY = polys.Min(p => new decimal[] { p[1], p[3], p[5], p[7] }.Min());
            decimal maxY = polys.Max(p => new decimal[] { p[1], p[3], p[5], p[7] }.Max());

            return new decimal[]
            {
                minX,
                minY,
                maxX - minX,
                maxY - minY
            };
        }
    }
}

