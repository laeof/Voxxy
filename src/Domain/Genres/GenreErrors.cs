using SharedKernel;

namespace Domain.Genres;

public static class GenreErrors
{
    public static Error NotFound(Guid genreId) => Error.NotFound(
        "Genre.NotFound",
        $"Genre with id {genreId} was not found.");
}