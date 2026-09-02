using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace AutoTechHub.Services
{
    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownService()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseBootstrap()
                .Build();
        }

        public string ToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            return Markdown.ToHtml(markdown, _pipeline);
        }

        public int CalculateReadingTime(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 1;

            // Average reading speed: 200 words per minute
            var words = Regex.Matches(content, @"\b\w+\b").Count;
            var minutes = (int)Math.Ceiling(words / 200.0);
            return Math.Max(1, minutes);
        }

        public string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return Guid.NewGuid().ToString("N").Substring(0, 8);

            // Remove Vietnamese accents
            string normalized = RemoveVietnameseAccents(title).ToLowerInvariant();

            // Replace non-alphanumeric with hyphen
            string slug = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(slug))
                slug = Guid.NewGuid().ToString("N").Substring(0, 8);

            return slug;
        }

        private string RemoveVietnameseAccents(string text)
        {
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                for (int j = 0; j < vietnameseSigns[i].Length; j++)
                    text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
            return text;
        }
    }
}
