namespace FoaieDeParcurs.Core.Domain;

public sealed class VerificationOptions
{
    public static VerificationOptions Default { get; } = new();

    /// <summary>
    /// How far apart two coordinates that are supposed to be "the same place" (a segment's end
    /// and the next segment's start, or a fill-up's station and its first segment) can be
    /// before it counts as a gap in the route.
    /// </summary>
    public double ContinuityToleranceMeters { get; init; } = 300;
}
