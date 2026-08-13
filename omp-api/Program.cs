using System.Threading.RateLimiting;
using BrahmCQRS.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using omp_api;
using omp_api.Handlers;
using omp_application.Contracts.Services;
using omp_application.Mappings;
using omp_application.Services;
using omp_infrastructure.Configuration;
using omp_infrastructure.Persistence;
using omp_infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Persistencia
// -----------------------------------------------------------------------------
builder.Services.AddDbContext<OmpDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.MigrationsAssembly("omp-infrastructure")));

// Los repositorios genéricos de BrahmCQRS dependen del tipo base DbContext.
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<OmpDbContext>());

// -----------------------------------------------------------------------------
// BrahmCQRS: repositorios y servicios CQRS genéricos, UnitOfWork, CurrentUser,
// ITimeProvider y EmailService (secciones "Mail" y "Rutas").
// TimeZoneInfo.Utc: todas las fechas se persisten en UTC (convención del proyecto);
// la conversión a CST México ocurre solo al presentar.
// -----------------------------------------------------------------------------
builder.Services.AddBrahmCQRSCore(builder.Configuration, TimeZoneInfo.Utc);

// -----------------------------------------------------------------------------
// BrahmCQRS: autenticación JWT con sesiones y revocación de tokens.
// -----------------------------------------------------------------------------
builder.Services.AddBrahmAuth(builder.Configuration);

// -----------------------------------------------------------------------------
// AutoMapper
// -----------------------------------------------------------------------------
builder.Services.AddAutoMapper(typeof(MappingProfile));

// -----------------------------------------------------------------------------
// reCAPTCHA para los endpoints públicos de la landing
// -----------------------------------------------------------------------------
builder.Services.AddMemoryCache();

builder.Services.Configure<ReCaptchaSettings>(
    builder.Configuration.GetSection(ReCaptchaSettings.SectionName));

builder.Services.AddHttpClient<ICaptchaValidator, CaptchaValidator>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "OrlinMonnarPhotography-API/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,
    AllowAutoRedirect = true
});

// -----------------------------------------------------------------------------
// Servicios de aplicación específicos del dominio
// -----------------------------------------------------------------------------
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IContactUsService, ContactUsService>();

// -----------------------------------------------------------------------------
// Almacenamiento y procesamiento de imágenes
// -----------------------------------------------------------------------------
builder.Services.Configure<StorageSettings>(
    builder.Configuration.GetSection(StorageSettings.SectionName));

// Singleton: sin estado por request y valida la configuración al arrancar,
// de modo que un RootPath mal puesto falla en el inicio y no en el primer upload.
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

// Un solo valor de configuración manda sobre todo el pipeline de subida.
var maxUploadBytes = builder.Configuration.GetValue("Storage:MaxUploadSizeMB", 25) * 1024L * 1024L;

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadBytes;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadBytes;
});

// -----------------------------------------------------------------------------
// Manejo global de excepciones
// -----------------------------------------------------------------------------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// -----------------------------------------------------------------------------
// Rate limiting (endpoints públicos anónimos y subida de imágenes)
// -----------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Formularios públicos anónimos (login, booking, contacto). 5/min por IP: son envíos
    // puntuales de usuario, no navegación; un límite bajo frena scripts/fuerza bruta sin
    // afectar el uso normal.
    options.AddPolicy(RateLimitPolicies.PublicForms, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Los GET públicos son anónimos y devuelven listados completos: el límite acota la
    // amplificación, no la navegación. 60/min por IP deja pasar una sesión normal de la
    // landing (categorías + carrusel + una galería) sin acercarse al tope; las 5/min de
    // PublicForms estrangularían la navegación.
    // Esto es la defensa contra abuso; la caché del servicio es rendimiento. Resuelven
    // cosas distintas y no se sustituyen.
    options.AddPolicy(RateLimitPolicies.PublicReads, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimitPolicies.Uploads, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// -----------------------------------------------------------------------------
// CORS: solo para desarrollo. En producción landing, panel y API comparten origen.
// -----------------------------------------------------------------------------
const string DevCorsPolicy = "AllowDevClients";

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        policy.WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// -----------------------------------------------------------------------------
// MVC y Swagger
// -----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Orlin Monnar Photography API",
        Version = "v1",
        Description = "API de citas, galería y contacto de Orlin Monnar Photography."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa únicamente el token JWT (sin el prefijo 'Bearer')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -----------------------------------------------------------------------------
// Pipeline HTTP
// -----------------------------------------------------------------------------

// Primero de todo: cualquier excepción aguas abajo sale como ProblemDetails.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCorsPolicy);
}

// En desarrollo no se redirige a HTTPS: el navegador no sigue redirecciones en
// las peticiones OPTIONS, por lo que el preflight de CORS de la landing falla.
// En producción el proxy sirve landing, panel y API bajo el mismo origen y HTTPS.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Los derivados de la galería se sirven como estáticos desde la raíz de Storage.
// En producción esto lo hace el reverse proxy, no Kestrel.
var storageSettings = app.Services.GetRequiredService<IOptions<StorageSettings>>().Value;

if (!string.IsNullOrWhiteSpace(storageSettings.RootPath))
{
    Directory.CreateDirectory(storageSettings.RootPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(storageSettings.RootPath)),
        RequestPath = storageSettings.PublicBaseUrl.TrimEnd('/'),
        OnPrepareResponse = context =>
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
    });
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
