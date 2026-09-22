using System.Threading.RateLimiting;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Implementation;
using Repository.Interface;
using Domain.Configuration;
using Quartz;
using Service.Implementation;
using Service.Interface;
using Service.Jobs;
using Web.Controllers;
using Web.Interceptor;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// --- Audit infrastructure (fills BaseAuditableEntity<string> fields on save) ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<AuditInterceptor>();

// --- Persistence ---
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlite(connectionString);
    options.UseLazyLoadingProxies();
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// --- Domain services ---
builder.Services.AddScoped<IPollutantService, PollutantService>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();
builder.Services.AddScoped<IInboundMeasurementEntryService, InboundMeasurementEntryService>();
builder.Services.AddScoped<IInboundMeasurementEntryProcessor, InboundMeasurementEntryProcessor>();
builder.Services.AddScoped<IEtlSyncService, EtlSyncService>();
builder.Services.AddScoped<IReferenceDataSeeder, ReferenceDataSeeder>();

// AddIdentityCore does not bring in data protection (AddIdentity would), and
// AddDefaultTokenProviders below needs IDataProtectionProvider to construct
// DataProtectorTokenProvider. Register it explicitly.
builder.Services.AddDataProtection();

// --- Identity (Option B: UserRole enum, no AspNetRoles tables; see ADR 0001) ---
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- External providers (Open-Meteo) ---
builder.Services.Configure<OpenMeteoOptions>(
    builder.Configuration.GetSection(OpenMeteoOptions.SectionName));

var openMeteo = builder.Configuration
                    .GetSection(OpenMeteoOptions.SectionName)
                    .Get<OpenMeteoOptions>()
                ?? new OpenMeteoOptions();

// HttpClient.Timeout stays infinite on purpose: the resilience handler owns all
// timeouts, and a shorter HttpClient.Timeout would cancel the pipeline mid-retry.
builder.Services.AddHttpClient<IGeocodingApiClient, GeocodingApiClient>(client =>
    {
        client.BaseAddress = new Uri(openMeteo.GeocodingBaseUrl);
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(openMeteo.RequestTimeoutSeconds);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(openMeteo.RequestTimeoutSeconds * 3);
        // Polly requires SamplingDuration >= 2 x AttemptTimeout.
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(openMeteo.RequestTimeoutSeconds * 4);
    });

builder.Services.AddHttpClient<IAirQualityApiClient, AirQualityApiClient>(client =>
    {
        client.BaseAddress = new Uri(openMeteo.AirQualityBaseUrl);
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddStandardResilienceHandler(options =>
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(openMeteo.RequestTimeoutSeconds);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(openMeteo.RequestTimeoutSeconds * 3);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(openMeteo.RequestTimeoutSeconds * 4);
    });


builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(StationsController.AddStationPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync(
            "Too many cities added from this address. Try again in a minute.", token);
    };
});

builder.Services.AddQuartzHostedService();

builder.Services.AddQuartz(options =>
{
    var jobKey = new JobKey("etl-sync-trigger", "etl");
    options.AddJob<EtlJob>(o => o.WithIdentity(jobKey));

    options.AddTrigger(o =>
    {
        var description = 
            "Uses IAirQualityApiClient to fetch latest air quality readings " + 
            "for all the stored stations in the db and sends inserts them into " +
            "the InboundMeasurementEntry repository that will be later used by " +
            "IInboundMeasurementEntryProcessor.";
        
        o.ForJob(jobKey).WithIdentity("etl-sync-trigger")
            .WithCronSchedule("0 0 * * * ?")
            .WithDescription(description);
    });
});

builder.Services.AddQuartz(options =>
{
    var jobKey = new JobKey("inbound-processor-trigger", "inbound");
    options.AddJob<InboundMeasurementEntryJob>(o => o.WithIdentity(jobKey));

    options.AddTrigger(o =>
    {
        var description = "Processing pending inbound entries";
        
        o.ForJob(jobKey).WithIdentity("inbound-processor-trigger")
            .WithCronSchedule("0 * * * * ?")
            .WithDescription(description);
    });
});


builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IReferenceDataSeeder>().SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();

app.UseAuthorization();
app.MapControllers();

app.Run();
