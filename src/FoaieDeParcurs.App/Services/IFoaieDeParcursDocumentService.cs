using FoaieDeParcurs.Core.Domain;

namespace FoaieDeParcurs.App.Services;

/// <summary>
/// Turns a saved fill-up into a <see cref="FoaieDeParcursDocument"/> or a rendered PDF file on
/// disk. Shared by the Fill-Up Detail screen's single-document preview/email flow and the
/// Dashboard's batch export, so both always build the document the same way.
/// </summary>
public interface IFoaieDeParcursDocumentService
{
    /// <summary>Null if the fill-up no longer exists.</summary>
    Task<FoaieDeParcursDocument?> BuildDocumentAsync(int fillUpId);

    /// <summary>Renders the fill-up's PDF to app-local storage and returns its path, or null if the fill-up no longer exists.</summary>
    Task<string?> BuildPdfAsync(int fillUpId);

    /// <summary>Substitutes {PeriodStart}/{PeriodEnd}/{DriverName} placeholders in an email subject/body template.</summary>
    string ApplyTemplate(string template, FoaieDeParcursDocument document);
}
