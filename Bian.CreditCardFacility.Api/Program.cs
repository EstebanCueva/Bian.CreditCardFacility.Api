using Microsoft.OpenApi.Models;
using Bian.CreditCardFacility.Api.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var proxyBaseUrl = builder.Configuration["Proxy:BaseUrl"] ?? "http://localhost:7002";

builder.Services.AddHttpClient("Proxy", c =>
{
    c.BaseAddress = new Uri(proxyBaseUrl);
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BIAN Credit Card Facility API",
        Version = "v1",
        Description = "PoC (fachada -> proxy -> legacy) - aligned to updated contract"
    });

    c.AddServer(new OpenApiServer
    {
        Url = "http://localhost:7003/api/bian/v1/"
    });

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

api.MapGet("/credit-card/customer/{CustomerId}/retrieve",
async (string CustomerId, HttpContext http, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
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
        "x-correlation-id",
        "x-channel-id",
        "x-application-id",
        "x-transaction-id",
        "x-parent-id"
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

    var client = httpClientFactory.CreateClient("Proxy");

    var proxyRequest = new HttpRequestMessage(
        HttpMethod.Get,
        "/api/proxy/v1/legacy-service/credit-card"
    );

    var canal = http.Request.Headers["x-channel-id"].ToString();
    proxyRequest.Headers.TryAddWithoutValidation("Canal", canal);

    HttpResponseMessage proxyResp;
    try
    {
        proxyResp = await client.SendAsync(proxyRequest, ct);
    }
    catch (TaskCanceledException)
    {
        return Results.StatusCode(504);
    }
    catch
    {
        return Results.StatusCode(502);
    }

    if (!proxyResp.IsSuccessStatusCode)
    {
        var errBody = await proxyResp.Content.ReadAsStringAsync(ct);
        return Results.Content(errBody, "application/json", Encoding.UTF8, (int)proxyResp.StatusCode);
    }

    RetrieveCreditCardFacilitiesByCustomer? data;
    try
    {
        data = await proxyResp.Content.ReadFromJsonAsync<RetrieveCreditCardFacilitiesByCustomer>(cancellationToken: ct);
    }
    catch
    {
        return Results.Problem("Invalid JSON from proxy", statusCode: 502);
    }

    if (data is null)
        return Results.Problem("Empty response from proxy", statusCode: 502);

    if (proxyResp.Headers.TryGetValues("Total-Count", out var totalCountValues))
        http.Response.Headers["Total-Count"] = totalCountValues.FirstOrDefault() ?? data.CreditCardFacilities.Count.ToString();
    else
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

    if (op.Responses.TryGetValue("200", out var r200))
    {
        r200.Headers ??= new Dictionary<string, OpenApiHeader>();
        r200.Headers["Total-Count"] = new OpenApiHeader
        {
            Description = "Total count of items available",
            Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
        };
    }

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
