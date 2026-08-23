using System.Reflection;
using PdfSharp.Fonts;

namespace FoaieDeParcurs.Pdf;

/// <summary>
/// PdfSharp has no GDI+ to fall back on outside Windows (Android included), so it needs an
/// explicit font resolver. Serves the OpenSans files embedded in this assembly — the only
/// fonts the renderer ever asks for.
/// </summary>
public sealed class OpenSansFontResolver : IFontResolver
{
    public const string FamilyName = "OpenSans";

    private static readonly Lazy<byte[]> Regular = new(() => ReadEmbeddedFont("OpenSans-Regular.ttf"));
    private static readonly Lazy<byte[]> Semibold = new(() => ReadEmbeddedFont("OpenSans-Semibold.ttf"));

    public string DefaultFontName => FamilyName;

    public byte[] GetFont(string faceName) =>
        faceName == BuildFaceName(bold: true) ? Semibold.Value : Regular.Value;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(BuildFaceName(isBold));

    private static string BuildFaceName(bool bold) => bold ? $"{FamilyName}#b" : FamilyName;

    private static byte[] ReadEmbeddedFont(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{assembly.GetName().Name}.Fonts.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font resource '{resourceName}' not found.");

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
