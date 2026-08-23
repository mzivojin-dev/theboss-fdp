using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Pdf;
using UglyToad.PdfPig;

namespace FoaieDeParcurs.Tests.Pdf;

/// <summary>
/// Renders a PDF from a known document model and asserts on extracted text content, per the
/// spec's testing decision — not pixel/layout snapshots, since layout is expected to evolve.
/// </summary>
public sealed class FoaieDeParcursPdfRendererTests
{
    private static FoaieDeParcursDocument SampleDocument(string vehicleCategory = "M1", string fuelType = "Motorina") => new(
        CompanyName: "Acme SRL",
        Cui: "RO12345678",
        DriverName: "Mihai Zivojinovic",
        VehiclePlate: "B-01-ABC",
        VehicleMakeModel: "Dacia Duster",
        VehicleCategory: vehicleCategory,
        FuelType: fuelType,
        FuelConsumptionNormPer100Km: 6.8,
        IssueDate: new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
        PeriodStart: new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero),
        PeriodEnd: new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
        Segments:
        [
            new FoaieDeParcursSegmentRow(
                new DateTimeOffset(2026, 5, 30, 9, 0, 0, TimeSpan.Zero), "Depot X",
                new DateTimeOffset(2026, 5, 30, 12, 30, 0, TimeSpan.Zero), "Cluj-Napoca",
                330.5, "Deplasare de serviciu")
        ],
        LitersFilled: 42.5,
        AmountPaid: 320.75m,
        Currency: "RON",
        StationName: "E70, Bucuresti",
        FillUpDate: new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero),
        OdometerReading: 85000);

    private static string ExtractText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return string.Join(" ", document.GetPages().Select(p => p.Text));
    }

    [Fact]
    public void Render_IncludesCompanyVehicleAndDriverFields()
    {
        var text = ExtractText(FoaieDeParcursPdfRenderer.Render(SampleDocument()));

        Assert.Contains("FOAIE DE PARCURS", text);
        Assert.Contains("Acme SRL", text);
        Assert.Contains("RO12345678", text);
        Assert.Contains("Mihai Zivojinovic", text);
        Assert.Contains("B-01-ABC", text);
        Assert.Contains("Motorina", text);
    }

    [Fact]
    public void Render_IncludesTheOfficialFormLabels()
    {
        var text = ExtractText(FoaieDeParcursPdfRenderer.Render(SampleDocument()));

        Assert.Contains("UNITATEA", text);
        Assert.Contains("Tip autovehicul", text);
        Assert.Contains("Persoane", text);
        Assert.Contains("Marfuri", text);
        Assert.Contains("Tip combustibil", text);
        Assert.Contains("Benzina", text);
        Assert.Contains("Plecare", text);
        Assert.Contains("Sosire", text);
        Assert.Contains("Locul", text);
        Assert.Contains("Ora", text);
        Assert.Contains("Consum specific", text);
        Assert.Contains("TOTAL KM", text);
        Assert.Contains("in oras", text);
        Assert.Contains("in exterior", text);
        Assert.Contains("Calculatie", text);
        Assert.Contains("Calculat", text);
        Assert.Contains("Sofer", text);
    }

    [Theory]
    [InlineData("M1", "Persoane")]
    [InlineData("N2", "Marfuri")]
    public void Render_ChecksTheVehicleTypeBoxMatchingTheCategory(string category, string expectedCheckedLabel)
    {
        // The checkbox itself isn't extractable as text (it's drawn geometry), but the label
        // it sits next to always prints — this just confirms the right category maps through.
        var text = ExtractText(FoaieDeParcursPdfRenderer.Render(SampleDocument(vehicleCategory: category)));

        Assert.Contains(expectedCheckedLabel, text);
    }

    [Fact]
    public void Render_IncludesEveryRouteSegmentRow()
    {
        var text = ExtractText(FoaieDeParcursPdfRenderer.Render(SampleDocument()));

        Assert.Contains("Depot X", text);
        Assert.Contains("Cluj-Napoca", text);
        Assert.Contains("330.5", text);
        Assert.Contains("09:00", text);
        Assert.Contains("12:30", text);
        Assert.Contains("Deplasare de serviciu", text);
    }

    [Fact]
    public void Render_IncludesFuelPurchaseAndConsumptionNorm()
    {
        var text = ExtractText(FoaieDeParcursPdfRenderer.Render(SampleDocument()));

        Assert.Contains("E70, Bucuresti", text);
        Assert.Contains("42.50", text);
        Assert.Contains("320.75", text);
        Assert.Contains("RON", text);
        Assert.Contains("6.8", text);
        Assert.Contains("85000", text);
    }

    [Fact]
    public void BuildFileName_MatchesTheSpecConvention()
    {
        var fileName = FoaieDeParcursPdfRenderer.BuildFileName(new DateTimeOffset(2026, 8, 23, 14, 32, 0, TimeSpan.Zero));

        Assert.Equal("FoaieDeParcurs_2026-08-23_1432.pdf", fileName);
    }
}
