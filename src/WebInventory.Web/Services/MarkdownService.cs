using Markdig;

namespace WebInventory.Web.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAdvancedExtensions()
        .Build();

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, _pipeline);
    }
}
