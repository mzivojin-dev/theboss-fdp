namespace FoaieDeParcurs.Core.Entities;

/// <summary>How often a Foaie de Parcurs document is generated.</summary>
public enum ReportingCadence
{
    /// <summary>One document per fill-up-to-fill-up interval.</summary>
    PerFillUp,

    /// <summary>One document per calendar month, bundling multiple fill-ups.</summary>
    Monthly
}
