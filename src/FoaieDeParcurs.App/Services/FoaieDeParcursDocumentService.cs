using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Repositories;
using FoaieDeParcurs.Pdf;

namespace FoaieDeParcurs.App.Services;

public sealed class FoaieDeParcursDocumentService(
    IFillUpRepository fillUpRepository,
    IRouteSegmentRepository routeSegmentRepository,
    IVehicleProfileRepository vehicleProfileRepository) : IFoaieDeParcursDocumentService
{
    public async Task<FoaieDeParcursDocument?> BuildDocumentAsync(int fillUpId)
    {
        var fillUp = await fillUpRepository.GetByIdAsync(fillUpId);
        if (fillUp is null)
        {
            return null;
        }

        var previousFillUp = await fillUpRepository.GetPreviousAsync(fillUp.Timestamp);
        var segments = await routeSegmentRepository.GetForFillUpAsync(fillUpId);
        var profile = await vehicleProfileRepository.GetOrCreateAsync();

        return FoaieDeParcursDocumentBuilder.Build(profile, fillUp, previousFillUp, segments);
    }

    public async Task<string?> BuildPdfAsync(int fillUpId)
    {
        var document = await BuildDocumentAsync(fillUpId);
        if (document is null)
        {
            return null;
        }

        var pdfDirectory = Path.Combine(FileSystem.AppDataDirectory, "pdfs");
        Directory.CreateDirectory(pdfDirectory);
        var path = Path.Combine(pdfDirectory, FoaieDeParcursPdfRenderer.BuildFileName(document.IssueDate));
        await File.WriteAllBytesAsync(path, FoaieDeParcursPdfRenderer.Render(document));

        return path;
    }

    public string ApplyTemplate(string template, FoaieDeParcursDocument document) => template
        .Replace("{PeriodStart}", document.PeriodStart.ToString("dd.MM.yyyy"))
        .Replace("{PeriodEnd}", document.PeriodEnd.ToString("dd.MM.yyyy"))
        .Replace("{DriverName}", document.DriverName);
}
