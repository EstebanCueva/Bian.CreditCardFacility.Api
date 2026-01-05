using Microsoft.OpenApi.Models;
using Bian.CreditCardFacility.Api;
using Bian.CreditCardFacility.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BIAN Credit Card Facility API",
        Version = "v1",
        Description = "PoC happy path (hardcoded data) - aligned to updated contract"
    });

    c.AddServer(new OpenApiServer
    {
        Url = "http://localhost:8080/api/bian/v1/"
    });

    // bearerAuth como en tu contrato
    c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Bearer token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "bearerAuth"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BIAN Credit Card Facility API v1");
});

var api = app.MapGroup("/api/bian/v1");

api.MapGet("/credit-card/Customer/{CustomerId}/retrieve", (string CustomerId, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(CustomerId))
    {
        return Results.BadRequest(new ErrorResponse
        {
            Code = "400",
            Type = "Validation",
            Message = "Invalid Customer ID format",
            Details = new List<string> { "CustomerId is required." }
        });
    }

    var requiredHeaders = new[]
    {
        "X-Correlation-ID",
        "X-Channel-Id",
        "X-Application-Id",
        "X-Transaction-Id",
        "X-Parent-Id"
    };

    var missing = requiredHeaders
        .Where(h => string.IsNullOrWhiteSpace(http.Request.Headers[h]))
        .ToList();

    if (missing.Count > 0)
    {
        return Results.BadRequest(new ErrorResponse
        {
            Code = "400",
            Type = "Validation",
            Message = "Missing required headers",
            Details = missing.Select(h => $"Header '{h}' is required.").ToList()
        });
    }

    if (!Data.TryGetByCustomerId(CustomerId, out var data))
    {
        return Results.NotFound(new ErrorResponse
        {
            Code = "404",
            Type = "Processing",
            Message = $"CustomerId '{CustomerId}' not found",
            Details = new List<string> { "No credit cards associated to the given customer id." }
        });
    }

    // 4) Total-Count header (contrato)
    http.Response.Headers["Total-Count"] = data.CreditCardFacilities.Count.ToString();

    return Results.Ok(data);
})
.WithTags("CR - CreditCardFacility")
.WithName("retrieveCreditCardFacility")
.Produces<RetrieveCreditCardFacilitiesByCustomer>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
.Produces<ErrorResponse>(StatusCodes.Status403Forbidden)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound)
.Produces<ErrorResponse>(StatusCodes.Status409Conflict)
.Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
.Produces<ErrorResponse>(StatusCodes.Status500InternalServerError)
.WithOpenApi(op =>
{
    // Headers (required)
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Correlation-ID",
        In = ParameterLocation.Header,
        Required = true,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Correlation identifier for the request"
    });
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Channel-Id",
        In = ParameterLocation.Header,
        Required = true,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Identifier for the channel making the request"
    });
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Application-Id",
        In = ParameterLocation.Header,
        Required = true,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Identifier for the application making the request"
    });
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Transaction-Id",
        In = ParameterLocation.Header,
        Required = true,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Identifier for the transaction"
    });
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Parent-Id",
        In = ParameterLocation.Header,
        Required = true,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Identifier for the parent transaction"
    });

    // Headers (optional)
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-App-Version",
        In = ParameterLocation.Header,
        Required = false,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Version of the application making the request"
    });
    op.Parameters.Add(new OpenApiParameter
    {
        Name = "X-Request-Id",
        In = ParameterLocation.Header,
        Required = false,
        Schema = new OpenApiSchema { Type = "string" },
        Description = "Unique identifier for the request"
    });

    // Response header Total-Count
    if (op.Responses.TryGetValue("200", out var r200))
    {
        r200.Headers ??= new Dictionary<string, OpenApiHeader>();
        r200.Headers["Total-Count"] = new OpenApiHeader
        {
            Description = "Total count of items available",
            Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
        };
    }

    // Security for operation (bearerAuth + {})
    op.Security ??= new List<OpenApiSecurityRequirement>();
    op.Security.Add(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" }
            },
            Array.Empty<string>()
        }
    });

    return op;
});

app.Run();
