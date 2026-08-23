namespace FoaieDeParcurs.Core.Domain;

/// <summary>
/// The outcome of <see cref="FillUpVerifier"/> — never silently "probably fine". When
/// <see cref="IsVerified"/> is false, <see cref="Issues"/> names exactly what's missing so the
/// driver can fix it rather than being left to guess.
/// </summary>
public sealed record VerificationResult(bool IsVerified, IReadOnlyList<string> Issues)
{
    public static VerificationResult Passed() => new(true, []);
    public static VerificationResult Failed(IReadOnlyList<string> issues) => new(false, issues);
}
