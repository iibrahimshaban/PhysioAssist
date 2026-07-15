using PhysioAssist.Api.Modules.DocumentationModule.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace PhysioAssist.Api.Modules.DocumentationModule.Helpers;

public static class DocumentationSummaryPdfRenderer
{
    public static byte[] Render(DocumentationSummary summary)
    {
        var isColleague = summary.Audience == SummaryAudience.Colleague;
        var title = isColleague ? "Case Summary" : "Your Progress Summary";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(20).Bold();
                    col.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("Generated: ").SemiBold();
                        text.Span(summary.CreatedAt.ToString("dd MMM yyyy"));
                    });

                    if (isColleague)
                    {
                        col.Item().PaddingTop(2).Text(text =>
                        {
                            text.Span("Scope: ").SemiBold();
                            text.Span(summary.Scope?.ToString() ?? "Full");
                        });
                        col.Item().PaddingTop(2).Text("This document has been de-identified for professional case review.")
                            .FontSize(9).Italic();
                    }

                    col.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingTop(20).Text(summary.SummaryText).LineHeight(1.4f);

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("PhysioAssist").FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }
}
