using FoaieDeParcurs.Core.Domain;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace FoaieDeParcurs.Pdf;

/// <summary>
/// Renders a <see cref="FoaieDeParcursDocument"/> to PDF bytes with PdfSharp — pure managed
/// code, no native library, so it actually runs on Android (QuestPDF's native Skia binary
/// doesn't — see ticket #8's notes). The thin translation-layer seam: takes a plain data
/// model, produces bytes, no persistence/platform dependency of its own.
/// </summary>
public static class FoaieDeParcursPdfRenderer
{
    private const double MarginPoints = 40;
    private const double PageWidthPoints = 595; // A4
    private const double PageHeightPoints = 842;

    private static readonly double[] ColumnOffsets = [0, 90, 220, 350, 400];
    private static readonly string[] ColumnHeaders = ["Data", "Plecare", "Sosire", "Km", "Scop"];

    private static bool _fontResolverRegistered;

    /// <summary>Filename convention from the spec: FoaieDeParcurs_yyyy-MM-dd_HHmm.pdf</summary>
    public static string BuildFileName(DateTimeOffset issueDate) => $"FoaieDeParcurs_{issueDate:yyyy-MM-dd_HHmm}.pdf";

    public static byte[] Render(FoaieDeParcursDocument document)
    {
        EnsureFontResolverRegistered();

        using var pdf = new PdfDocument();
        var page = pdf.AddPage();
        page.Width = XUnit.FromPoint(PageWidthPoints);
        page.Height = XUnit.FromPoint(PageHeightPoints);

        using var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont(OpenSansFontResolver.FamilyName, 18, XFontStyleEx.Bold);
        var headingFont = new XFont(OpenSansFontResolver.FamilyName, 12, XFontStyleEx.Bold);
        var bodyFont = new XFont(OpenSansFontResolver.FamilyName, 10, XFontStyleEx.Regular);
        var tableHeaderFont = new XFont(OpenSansFontResolver.FamilyName, 9, XFontStyleEx.Bold);
        var tableBodyFont = new XFont(OpenSansFontResolver.FamilyName, 9, XFontStyleEx.Regular);

        double y = MarginPoints;
        double x = MarginPoints;
        var contentWidth = PageWidthPoints - 2 * MarginPoints;

        gfx.DrawString("Foaie de Parcurs", titleFont, XBrushes.Black, new XPoint(x, y));
        y += 26;
        y = DrawLine(gfx, bodyFont, x, y, $"Data emiterii: {document.IssueDate:dd.MM.yyyy}");
        y = DrawLine(gfx, bodyFont, x, y, $"Perioada: {document.PeriodStart:dd.MM.yyyy HH:mm} - {document.PeriodEnd:dd.MM.yyyy HH:mm}");

        y += 10;
        y = DrawLine(gfx, headingFont, x, y, "Societate si vehicul");
        y = DrawLine(gfx, bodyFont, x, y, $"Societate: {document.CompanyName}    CUI: {document.Cui}");
        y = DrawLine(gfx, bodyFont, x, y, $"Conducator auto: {document.DriverName}");
        y = DrawLine(gfx, bodyFont, x, y, $"Vehicul: {document.VehiclePlate} - {document.VehicleMakeModel} (categoria {document.VehicleCategory})");
        y = DrawLine(gfx, bodyFont, x, y, $"Tip combustibil: {document.FuelType}    Norma proprie de consum: {document.FuelConsumptionNormPer100Km:0.0} L/100km");

        y += 14;
        y = DrawTable(gfx, tableHeaderFont, tableBodyFont, x, y, contentWidth, document.Segments);

        y += 6;
        y = DrawLine(gfx, headingFont, x, y, $"Total km: {document.TotalDistanceKm:0.0}");

        y += 10;
        y = DrawLine(gfx, headingFont, x, y, "Alimentare combustibil");
        y = DrawLine(gfx, bodyFont, x, y, $"Statie: {document.StationName}    Data: {document.FillUpDate:dd.MM.yyyy HH:mm}");
        y = DrawLine(gfx, bodyFont, x, y, $"Cantitate: {document.LitersFilled:0.00} L    Suma: {document.AmountPaid:0.00} {document.Currency}");
        if (document.OdometerReading is double odometer)
        {
            DrawLine(gfx, bodyFont, x, y, $"Kilometraj bord: {odometer:0} km");
        }

        using var stream = new MemoryStream();
        pdf.Save(stream);
        return stream.ToArray();
    }

    private static double DrawLine(XGraphics gfx, XFont font, double x, double y, string text)
    {
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(x, y));
        return y + font.Height + 4;
    }

    private static double DrawTable(
        XGraphics gfx, XFont headerFont, XFont bodyFont, double x, double y, double width,
        IReadOnlyList<FoaieDeParcursSegmentRow> rows)
    {
        var rowHeight = bodyFont.Height + 6;

        void DrawRow(double rowY, string[] cells, XFont font)
        {
            for (var i = 0; i < cells.Length; i++)
            {
                gfx.DrawString(cells[i], font, XBrushes.Black, new XPoint(x + ColumnOffsets[i], rowY));
            }
        }

        gfx.DrawLine(XPens.Black, x, y, x + width, y);
        y += 4;
        DrawRow(y, ColumnHeaders, headerFont);
        y += rowHeight;
        gfx.DrawLine(XPens.Black, x, y, x + width, y);
        y += 4;

        foreach (var row in rows)
        {
            var cells = new[]
            {
                row.Date.ToString("dd.MM.yyyy HH:mm"),
                row.From,
                row.To,
                row.DistanceKm.ToString("0.0"),
                row.Purpose
            };
            DrawRow(y, cells, bodyFont);
            y += rowHeight;
        }

        gfx.DrawLine(XPens.Black, x, y, x + width, y);
        return y + 4;
    }

    private static void EnsureFontResolverRegistered()
    {
        if (_fontResolverRegistered)
        {
            return;
        }

        GlobalFontSettings.FontResolver = new OpenSansFontResolver();
        _fontResolverRegistered = true;
    }
}
