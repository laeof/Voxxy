using Infrastructure.Database;

namespace Web.Api.Extensions;

public static class SeedExtensions
{
    public static void ApplySeeding(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.EnsureCreated();
    }
}