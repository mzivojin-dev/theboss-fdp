using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FoaieDeParcurs.Data.Repositories;

public sealed class FillUpRepository(AppDbContext db) : IFillUpRepository
{
    public async Task<List<FillUp>> GetAllAsync() =>
        await db.FillUps.AsNoTracking().OrderByDescending(f => f.Timestamp).ToListAsync();

    public async Task<FillUp?> GetByIdAsync(int id) =>
        await db.FillUps.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id);

    public async Task<FillUp?> GetMostRecentAsync() =>
        await db.FillUps.AsNoTracking().OrderByDescending(f => f.Timestamp).FirstOrDefaultAsync();

    public async Task<FillUp?> GetPreviousAsync(DateTimeOffset beforeTimestamp) =>
        await db.FillUps.AsNoTracking()
            .Where(f => f.Timestamp < beforeTimestamp)
            .OrderByDescending(f => f.Timestamp)
            .FirstOrDefaultAsync();

    public async Task<FillUp> AddWithSegmentsAsync(FillUp fillUp, IReadOnlyList<RouteSegment> segments)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        db.FillUps.Add(fillUp);
        await db.SaveChangesAsync();

        foreach (var segment in segments)
        {
            segment.EndFillUpId = fillUp.Id;
            db.RouteSegments.Add(segment);
        }
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
        return fillUp;
    }

    public async Task UpdateWithSegmentsAsync(FillUp fillUp, IReadOnlyList<RouteSegment> segments)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        db.FillUps.Update(fillUp);
        await db.SaveChangesAsync();

        await db.RouteSegments.Where(s => s.EndFillUpId == fillUp.Id).ExecuteDeleteAsync();
        foreach (var segment in segments)
        {
            segment.Id = 0;
            segment.EndFillUpId = fillUp.Id;
            db.RouteSegments.Add(segment);
        }
        await db.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task SetVerifiedAsync(int id, bool isVerified) =>
        await db.FillUps.Where(f => f.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.IsVerified, isVerified));

    public async Task SetEmailSentAsync(int id, bool emailSent) =>
        await db.FillUps.Where(f => f.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.EmailSent, emailSent));

    public async Task DeleteAsync(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        await db.RouteSegments.Where(s => s.EndFillUpId == id).ExecuteDeleteAsync();
        await db.RouteSegments.Where(s => s.StartFillUpId == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.StartFillUpId, (int?)null));
        await db.FillUps.Where(f => f.Id == id).ExecuteDeleteAsync();

        await transaction.CommitAsync();
    }
}
