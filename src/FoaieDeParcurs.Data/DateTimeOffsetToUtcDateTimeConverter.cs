using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoaieDeParcurs.Data;

/// <summary>See the comment on <see cref="AppDbContext.ConfigureConventions"/>.</summary>
public sealed class DateTimeOffsetToUtcDateTimeConverter()
    : ValueConverter<DateTimeOffset, DateTime>(
        v => v.UtcDateTime,
        v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));
