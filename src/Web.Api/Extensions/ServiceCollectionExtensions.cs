using Microsoft.OpenApi.Models;

namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(o =>
        {
            o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            var cookieScheme = new OpenApiSecurityScheme
            {
                Name = "access_token",
                In = ParameterLocation.Cookie,
                Type = SecuritySchemeType.ApiKey,
                Description = "Enter your authentication cookie value"
            };

            o.AddSecurityDefinition("cookieAuth", cookieScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "cookieAuth"
                        }
                    },
                    Array.Empty<string>()
                }
            };

            o.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}
