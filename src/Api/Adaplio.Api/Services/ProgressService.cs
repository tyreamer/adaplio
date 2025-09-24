using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Progress;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Adaplio.Api.Services;

public interface IProgressService
{
    Task<WeeklyAdherence[]> GetClientAdherenceAsync(int clientProfileId, int? weeks = null);
    Task<decimal> CalculateOverallAdherenceAsync(int clientProfileId);
    Task UpdateAdherenceWeekAsync(int clientProfileId, DateOnly weekStart);
}

public class ProgressService : IProgressService
{
    private readonly AppDbContext _context;

    public ProgressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WeeklyAdherence[]> GetClientAdherenceAsync(int clientProfileId, int? weeks = null)
    {
        var query = _context.AdherenceWeeks
            .Where(aw => aw.ClientProfileId == clientProfileId)
            .OrderByDescending(aw => aw.Year)
            .ThenByDescending(aw => aw.WeekNumber);

        if (weeks.HasValue)
        {
            query = (IOrderedQueryable<AdherenceWeek>)query.Take(weeks.Value);
        }

        var adherenceWeeks = await query.ToListAsync();

        return adherenceWeeks.Select(aw => new WeeklyAdherence(
            aw.Year,
            aw.WeekNumber,
            aw.WeekStartDate,
            aw.AdherencePercentage,
            aw.TotalExercisesCompleted,
            aw.TotalExercisesPlanned
        )).ToArray();
    }

    public async Task<decimal> CalculateOverallAdherenceAsync(int clientProfileId)
    {
        var adherenceWeeks = await _context.AdherenceWeeks
            .Where(aw => aw.ClientProfileId == clientProfileId)
            .ToListAsync();

        if (!adherenceWeeks.Any())
            return 0;

        var totalCompleted = adherenceWeeks.Sum(aw => aw.TotalExercisesCompleted);
        var totalPlanned = adherenceWeeks.Sum(aw => aw.TotalExercisesPlanned);

        return totalPlanned > 0 ? Math.Round((decimal)totalCompleted / totalPlanned * 100, 1) : 0;
    }

    public async Task UpdateAdherenceWeekAsync(int clientProfileId, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        var calendar = CultureInfo.CurrentCulture.Calendar;
        var year = weekStart.Year;
        var weekNumber = calendar.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue),
            CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        // Get all exercise instances for this week
        var exerciseInstances = await _context.ExerciseInstances
            .Include(ei => ei.PlanInstance)
            .Where(ei => ei.PlanInstance.ClientProfileId == clientProfileId)
            .ToListAsync();

        // Filter by week number (since we don't have scheduled dates)
        var targetWeekNumber = calendar.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue),
            CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        exerciseInstances = exerciseInstances
            .Where(ei => ei.WeekNumber == targetWeekNumber)
            .ToList();

        var plannedCount = exerciseInstances.Count;

        // Count completed exercises (including partial completions with hold factor)
        var completedCount = 0m;
        foreach (var ei in exerciseInstances)
        {
            var progressEvents = await _context.ProgressEvents
                .Where(pe => pe.ExerciseInstanceId == ei.Id)
                .ToListAsync();

            if (progressEvents.Any())
            {
                var hasCompletedEvent = progressEvents.Any(pe => pe.EventType == "exercise_completed");
                if (hasCompletedEvent)
                {
                    completedCount += 1.0m;
                }
            }
        }

        var adherencePercentage = plannedCount > 0 ? Math.Round(completedCount / plannedCount * 100, 1) : 0;

        // Find or create adherence week record
        var adherenceWeek = await _context.AdherenceWeeks
            .FirstOrDefaultAsync(aw => aw.ClientProfileId == clientProfileId &&
                                     aw.Year == year &&
                                     aw.WeekNumber == weekNumber);

        if (adherenceWeek == null)
        {
            adherenceWeek = new AdherenceWeek
            {
                ClientProfileId = clientProfileId,
                Year = year,
                WeekNumber = weekNumber,
                WeekStartDate = weekStart
            };
            _context.AdherenceWeeks.Add(adherenceWeek);
        }

        adherenceWeek.TotalExercisesPlanned = plannedCount;
        adherenceWeek.TotalExercisesCompleted = (int)Math.Round(completedCount);
        adherenceWeek.UpdatedAt = DateTimeOffset.UtcNow;
        adherenceWeek.AdherencePercentage = adherencePercentage;

        await _context.SaveChangesAsync();
    }
}