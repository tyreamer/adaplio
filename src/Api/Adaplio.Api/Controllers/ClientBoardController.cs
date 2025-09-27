using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Adaplio.Api.Controllers;

[ApiController]
[Route("api/client")]
[Authorize]
public class ClientBoardController : ControllerBase
{
    private readonly ILogger<ClientBoardController> _logger;

    public ClientBoardController(ILogger<ClientBoardController> logger)
    {
        _logger = logger;
    }

    [HttpGet("board")]
    public async Task<IActionResult> GetBoard([FromQuery] DateOnly weekStart)
    {
        try
        {
            // For now, return mock data that matches the frontend expectations
            // In a real implementation, this would query the database for the user's exercise plan

            var board = GenerateMockBoard(weekStart);
            return Ok(board);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client board for week {WeekStart}", weekStart);
            return StatusCode(500, "Failed to load board");
        }
    }

    [HttpPost("board/quick-log")]
    public async Task<IActionResult> QuickLog([FromBody] QuickLogRequest request)
    {
        try
        {
            // In a real implementation, this would update the exercise instance in the database
            _logger.LogInformation("Quick log for exercise instance {ExerciseInstanceId}: {Completed}",
                request.ExerciseInstanceId, request.Completed);

            // For now, just return success
            return Ok(new { Message = "Exercise logged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging exercise {ExerciseInstanceId}", request.ExerciseInstanceId);
            return StatusCode(500, "Failed to log exercise");
        }
    }

    private static BoardResponse GenerateMockBoard(DateOnly weekStart)
    {
        var days = new List<DayBoardResponse>();

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var dayName = date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Monday",
                DayOfWeek.Tuesday => "Tuesday",
                DayOfWeek.Wednesday => "Wednesday",
                DayOfWeek.Thursday => "Thursday",
                DayOfWeek.Friday => "Friday",
                DayOfWeek.Saturday => "Saturday",
                DayOfWeek.Sunday => "Sunday",
                _ => "Unknown"
            };

            var exercises = GenerateMockExercises(date, i);

            days.Add(new DayBoardResponse
            {
                DayName = dayName,
                Date = date,
                DayOfWeek = (int)date.DayOfWeek,
                Exercises = exercises
            });
        }

        return new BoardResponse
        {
            WeekStart = weekStart,
            WeekEnd = weekStart.AddDays(6),
            Days = days.ToArray()
        };
    }

    private static ExerciseCardResponse[] GenerateMockExercises(DateOnly date, int dayIndex)
    {
        // Generate different exercise patterns for different days
        // Skip some days to simulate rest days
        if (dayIndex == 2 || dayIndex == 6) // Wednesday and Sunday rest days
        {
            return Array.Empty<ExerciseCardResponse>();
        }

        var exercises = new List<ExerciseCardResponse>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Determine status based on date relative to today
        string GetStatusForDate(DateOnly exerciseDate)
        {
            if (exerciseDate < today) return "done";
            if (exerciseDate == today) return "planned";
            return "planned";
        }

        switch (dayIndex)
        {
            case 0: // Monday - Upper body
                exercises.AddRange(new[]
                {
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 1,
                        ExerciseName = "Push-ups",
                        TargetSets = 3,
                        TargetReps = 12,
                        Status = GetStatusForDate(date),
                        Notes = "Keep your core engaged"
                    },
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 2,
                        ExerciseName = "Shoulder Shrugs",
                        TargetSets = 2,
                        TargetReps = 15,
                        Status = GetStatusForDate(date)
                    }
                });
                break;

            case 1: // Tuesday - Lower body
                exercises.AddRange(new[]
                {
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 1,
                        ExerciseName = "Squats",
                        TargetSets = 3,
                        TargetReps = 10,
                        Status = GetStatusForDate(date),
                        Notes = "Focus on proper form"
                    },
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 2,
                        ExerciseName = "Calf Raises",
                        TargetSets = 2,
                        TargetReps = 20,
                        Status = GetStatusForDate(date)
                    },
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 3,
                        ExerciseName = "Wall Sit",
                        TargetSets = 1,
                        HoldSeconds = 30,
                        Status = GetStatusForDate(date),
                        Notes = "Hold position steadily"
                    }
                });
                break;

            case 3: // Thursday - Core
                exercises.AddRange(new[]
                {
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 1,
                        ExerciseName = "Plank",
                        TargetSets = 3,
                        HoldSeconds = 30,
                        Status = GetStatusForDate(date),
                        Notes = "Keep body in straight line"
                    },
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 2,
                        ExerciseName = "Dead Bug",
                        TargetSets = 2,
                        TargetReps = 8,
                        Status = GetStatusForDate(date),
                        Notes = "Each side"
                    }
                });
                break;

            case 4: // Friday - Full body
                exercises.AddRange(new[]
                {
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 1,
                        ExerciseName = "Burpees",
                        TargetSets = 2,
                        TargetReps = 5,
                        Status = GetStatusForDate(date),
                        Notes = "Take your time"
                    },
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 2,
                        ExerciseName = "Mountain Climbers",
                        TargetSets = 3,
                        TargetReps = 10,
                        Status = GetStatusForDate(date),
                        Notes = "Each leg"
                    }
                });
                break;

            case 5: // Saturday - Flexibility
                exercises.AddRange(new[]
                {
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 1,
                        ExerciseName = "Hip Flexor Stretch",
                        TargetSets = 2,
                        HoldSeconds = 30,
                        Status = GetStatusForDate(date),
                        Notes = "Each side, gentle stretch"
                    },
                    new ExerciseCardResponse
                    {
                        ExerciseInstanceId = dayIndex * 10 + 2,
                        ExerciseName = "Cat-Cow Stretch",
                        TargetSets = 1,
                        TargetReps = 10,
                        Status = GetStatusForDate(date),
                        Notes = "Slow and controlled movement"
                    }
                });
                break;
        }

        return exercises.ToArray();
    }
}

public class BoardResponse
{
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }
    public DayBoardResponse[] Days { get; set; } = Array.Empty<DayBoardResponse>();
}

public class DayBoardResponse
{
    public string DayName { get; set; } = "";
    public DateOnly Date { get; set; }
    public int DayOfWeek { get; set; }
    public ExerciseCardResponse[] Exercises { get; set; } = Array.Empty<ExerciseCardResponse>();
}

public class ExerciseCardResponse
{
    public int ExerciseInstanceId { get; set; }
    public string ExerciseName { get; set; } = "";
    public string? ExerciseDescription { get; set; }
    public int? TargetSets { get; set; }
    public int? TargetReps { get; set; }
    public int? HoldSeconds { get; set; }
    public string Status { get; set; } = "";
    public int? CompletedSets { get; set; }
    public int? CompletedReps { get; set; }
    public int? CompletedHoldSeconds { get; set; }
    public string? Notes { get; set; }
}

public class QuickLogRequest
{
    public int ExerciseInstanceId { get; set; }
    public bool Completed { get; set; }
}