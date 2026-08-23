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
///
/// Layout matches the official printed "Foaie de Parcurs" form (unitate header, tip
/// autovehicul / tip combustibil checkboxes, Plecare/Sosire Locul+Ora columns, consum
/// specific / TOTAL KM footer with the "in oras / in exterior" split and the calculatie
/// formula note) so the generated PDF is visually recognizable against the paper original.
/// Fields the app doesn't track (Nr. act de insotire, the in oras/in exterior split, the
/// calculatie result, and the Calculat/Sofer signatures) are printed blank rather than
/// guessed — this is a legally significant tax document, so a blank box beats a wrong number.
///
/// All Y coordinates in this file are "top of the next free line" — every draw call happens
/// at the current y, then y is advanced by that line's own height before anything else is
/// drawn, so lines/text never overlap regardless of font metrics.
/// </summary>
public static class FoaieDeParcursPdfRenderer
{
    private const double MarginPoints = 36;
    private const double PageWidthPoints = 595; // A4
    private const double PageHeightPoints = 842;
    private const double CheckboxSize = 8;

    // Column x-offsets (relative to the table's left edge) for the two-row header:
    // Data | Plecare(Locul, Ora) | Sosire(Locul, Ora) | KM | Nr. act de insotire | Observatii
    private static readonly double[] ColumnOffsets = [0, 50, 150, 185, 285, 320, 355, 425];

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
        var titleFont = new XFont(OpenSansFontResolver.FamilyName, 16, XFontStyleEx.Bold);
        var headingFont = new XFont(OpenSansFontResolver.FamilyName, 10, XFontStyleEx.Bold);
        var bodyFont = new XFont(OpenSansFontResolver.FamilyName, 9, XFontStyleEx.Regular);
        var smallFont = new XFont(OpenSansFontResolver.FamilyName, 7.5, XFontStyleEx.Regular);
        var tableHeaderFont = new XFont(OpenSansFontResolver.FamilyName, 7.5, XFontStyleEx.Bold);
        var tableBodyFont = new XFont(OpenSansFontResolver.FamilyName, 7.5, XFontStyleEx.Regular);

        double y = MarginPoints;
        double x = MarginPoints;
        var contentWidth = PageWidthPoints - 2 * MarginPoints;
        var checkboxLineHeight = LineHeight(bodyFont) + 2;

        // --- Title + UNITATEA ---
        gfx.DrawString("FOAIE DE PARCURS", titleFont, XBrushes.Black,
            new XRect(x, y, contentWidth, LineHeight(titleFont)), XStringFormats.TopCenter);
        y += LineHeight(titleFont) + 8;
        y = DrawLine(gfx, bodyFont, x, y, $"UNITATEA: {document.CompanyName}    CUI: {document.Cui}");
        y += 6;

        // --- Tip autovehicul checkboxes ---
        var category = document.VehicleCategory.Trim().ToUpperInvariant();
        var isPersoane = category.StartsWith('M');
        var isMarfuri = category.StartsWith('N');
        var cx = DrawLabel(gfx, bodyFont, x, y, "Tip autovehicul:");
        cx = DrawCheckbox(gfx, bodyFont, cx + 8, y, "Persoane", isPersoane);
        DrawCheckbox(gfx, bodyFont, cx + 20, y, "Marfuri", isMarfuri);
        y += checkboxLineHeight;

        // --- Tip combustibil checkboxes ---
        var isBenzina = string.Equals(document.FuelType, "Benzina", StringComparison.OrdinalIgnoreCase);
        var isMotorina = string.Equals(document.FuelType, "Motorina", StringComparison.OrdinalIgnoreCase);
        cx = DrawLabel(gfx, bodyFont, x, y, "Tip combustibil:");
        cx = DrawCheckbox(gfx, bodyFont, cx + 8, y, "Benzina", isBenzina);
        DrawCheckbox(gfx, bodyFont, cx + 20, y, "Motorina", isMotorina);
        y += checkboxLineHeight + 6;

        // --- Numar auto / Nume sofer (document-level fields, sit above the route table) ---
        gfx.DrawLine(XPens.Black, x, y, x + contentWidth, y);
        y += Ascent(bodyFont) + 2;
        y = DrawLine(gfx, bodyFont, x, y,
            $"Numar auto: {document.VehiclePlate}    Nume sofer: {document.DriverName}");
        y += 3;
        gfx.DrawLine(XPens.Black, x, y, x + contentWidth, y);
        y += Ascent(tableHeaderFont) + 2;

        y = DrawTable(gfx, tableHeaderFont, tableBodyFont, x, y, contentWidth, document.Segments);

        // --- Footer: consum specific / TOTAL KM / in oras-in exterior split ---
        y += 8;
        y = DrawLine(gfx, bodyFont, x, y,
            $"Consum specific: {document.FuelConsumptionNormPer100Km:0.0} L/100km    TOTAL KM: {document.TotalDistanceKm:0.0}");
        y = DrawLine(gfx, bodyFont, x, y, "Din care: in oras _______________    in exterior _______________");
        y += 4;
        y = DrawLine(gfx, smallFont, x, y, "Calculatie: _______________ (nr. de km x 10% consum specific x pret combustibil)");
        y += 14;
        y = DrawLine(gfx, bodyFont, x, y, "Calculat, _______________________          Sofer _______________________");

        // --- Fuel purchase details (legally required, not part of the original paper layout) ---
        y += 14;
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

    /// <summary>The vertical space one line of this font actually needs, with a little breathing room.</summary>
    private static double LineHeight(XFont font) => font.Height + 2;

    /// <summary>
    /// Approximate ascent (how far glyphs reach above the baseline) — PdfSharp's XFont doesn't
    /// expose this directly, and text is baseline-positioned (see <see cref="DrawCheckbox"/>'s
    /// remarks) while lines/rectangles are not. A line drawn less than this far above the next
    /// line of text gets cut through by that text's own ascenders — confirmed by dumping the
    /// raw PDF content stream and comparing operator coordinates.
    /// </summary>
    private static double Ascent(XFont font) => font.Height * 0.75;

    private static double DrawLine(XGraphics gfx, XFont font, double x, double y, string text)
    {
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(x, y));
        return y + LineHeight(font) + 4;
    }

    private static double DrawLabel(XGraphics gfx, XFont font, double x, double y, string text)
    {
        gfx.DrawString(text, font, XBrushes.Black, new XPoint(x, y));
        return x + gfx.MeasureString(text, font).Width;
    }

    /// <summary>
    /// Draws a small checkbox (crossed if checked) aligned with a text label's line, followed
    /// by the label; returns the x position right after the label.
    ///
    /// <see cref="XGraphics.DrawString(string, XFont, XBrush, XPoint)"/> treats its XPoint as
    /// the text *baseline*, but <see cref="XGraphics.DrawRectangle(XPen, XRect)"/> treats its
    /// XRect's Y as a literal top-down page coordinate with no such adjustment — confirmed by
    /// dumping the raw PDF content stream operators. So a checkbox positioned at the label's
    /// same Y would hang below the baseline, into the next line, rather than sitting next to
    /// the text. Shifting the box up by roughly its own height plus a hair puts it level with
    /// the text's cap-height instead.
    /// </summary>
    private static double DrawCheckbox(XGraphics gfx, XFont font, double x, double y, string label, bool isChecked)
    {
        var boxTop = y - CheckboxSize + 1;
        var rect = new XRect(x, boxTop, CheckboxSize, CheckboxSize);
        gfx.DrawRectangle(XPens.Black, rect);
        if (isChecked)
        {
            gfx.DrawLine(XPens.Black, rect.Left, rect.Top, rect.Right, rect.Bottom);
            gfx.DrawLine(XPens.Black, rect.Left, rect.Bottom, rect.Right, rect.Top);
        }

        var labelX = x + CheckboxSize + 4;
        gfx.DrawString(label, font, XBrushes.Black, new XPoint(labelX, y));
        return labelX + gfx.MeasureString(label, font).Width;
    }

    private static double DrawTable(
        XGraphics gfx, XFont headerFont, XFont bodyFont, double x, double y, double width,
        IReadOnlyList<FoaieDeParcursSegmentRow> rows)
    {
        var headerRowHeight = LineHeight(headerFont) + 2;
        var bodyRowHeight = LineHeight(bodyFont) + 2;

        void DrawRow(double rowY, XFont font, params string[] cells)
        {
            for (var i = 0; i < cells.Length; i++)
            {
                gfx.DrawString(cells[i], font, XBrushes.Black, new XPoint(x + ColumnOffsets[i], rowY));
            }
        }

        // Top header row: Data | Plecare (spans Locul+Ora) | Sosire (spans Locul+Ora) | KM | Nr. act de insotire | Observatii
        DrawRow(y, headerFont, "Data", "Plecare", "", "Sosire", "", "KM", "Nr. act", "Observatii");
        y += headerRowHeight;

        // Sub-header row: Locul / Ora under each of Plecare and Sosire; "de insotire" continues "Nr. act" from above
        DrawRow(y, headerFont, "", "Locul", "Ora", "Locul", "Ora", "", "de insotire", "");
        y += headerRowHeight;

        gfx.DrawLine(XPens.Black, x, y, x + width, y);
        y += Ascent(bodyFont) + 2;

        foreach (var row in rows)
        {
            DrawRow(y, bodyFont,
                row.DepartureTimestamp.ToString("dd.MM.yyyy"),
                row.DepartureLocation,
                row.DepartureTimestamp.ToString("HH:mm"),
                row.ArrivalLocation,
                row.ArrivalTimestamp.ToString("HH:mm"),
                row.DistanceKm.ToString("0.0"),
                string.Empty,
                row.Observations);
            y += bodyRowHeight;
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
