namespace PhysioAssist.Api.Shared.Dtos.Pdf;

public record PdfDocumentContent(
    string Title,
    List<PdfSection> Sections,
    string? FooterText = null);

public record PdfSection(string? Heading, List<string> Paragraphs, byte[]? ImageBytes = null);
