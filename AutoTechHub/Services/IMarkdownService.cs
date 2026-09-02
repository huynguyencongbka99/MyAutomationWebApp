namespace AutoTechHub.Services
{
    public interface IMarkdownService
    {
        string ToHtml(string markdown);
        int CalculateReadingTime(string content);
        string GenerateSlug(string title);
    }
}
