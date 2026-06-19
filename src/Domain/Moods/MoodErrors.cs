using SharedKernel;

namespace Domain.Moods;

public static class MoodErrors
{
    public static Error NotFound(Guid moodId) => Error.NotFound(
        "Mood.NotFound",
        $"Mood with id {moodId} was not found.");
}