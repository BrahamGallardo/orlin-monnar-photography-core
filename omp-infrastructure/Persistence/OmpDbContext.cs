using BrahmCQRS.Application.Contracts.Services;
using BrahmCQRS.Domain.Entities;
using BrahmCQRS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using omp_domain.Entities;

namespace omp_infrastructure.Persistence;

/// <summary>
/// Contexto de base de datos de Orlin Monnar Photography.
/// </summary>
/// <remarks>
/// Hereda de <see cref="BaseDbContext"/>, que puebla automáticamente los campos
/// de auditoría de las entidades derivadas de <see cref="BaseEntity"/>.
/// Todas las fechas se persisten en UTC.
/// </remarks>
public class OmpDbContext(
    ICurrentUserService currentUserService,
    ITimeProvider timeProvider,
    DbContextOptions<OmpDbContext> options) : BaseDbContext(currentUserService, timeProvider, options)
{
    /// <summary>
    /// Restablece DateTimeKind.Utc al leer, porque datetime2 no conserva el Kind.
    /// </summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    /// <summary>
    /// Variante del conversor UTC para propiedades de fecha opcionales.
    /// </summary>
    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter =
        new(
            value => value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value);

    #region Auth DbSets

    /// <summary>
    /// Usuarios de autenticación.
    /// </summary>
    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();

    /// <summary>
    /// Roles de autorización.
    /// </summary>
    public DbSet<AuthRole> AuthRoles => Set<AuthRole>();

    /// <summary>
    /// Sesiones activas.
    /// </summary>
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();

    /// <summary>
    /// Tokens revocados.
    /// </summary>
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    #endregion

    #region Business DbSets

    /// <summary>
    /// Paquetes fotográficos.
    /// </summary>
    public DbSet<Package> Packages => Set<Package>();

    /// <summary>
    /// Citas agendadas.
    /// </summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <summary>
    /// Categorías de la galería.
    /// </summary>
    public DbSet<GalleryCategory> GalleryCategories => Set<GalleryCategory>();

    /// <summary>
    /// Fotografías.
    /// </summary>
    public DbSet<Photo> Photos => Set<Photo>();

    /// <summary>
    /// Mensajes del formulario de contacto.
    /// </summary>
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    #endregion

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAuth(modelBuilder);
        ConfigurePackages(modelBuilder);
        ConfigureAppointments(modelBuilder);
        ConfigureGallery(modelBuilder);
        ConfigureContactMessages(modelBuilder);

        SeedAuthData(modelBuilder);

        // Debe ejecutarse al final, cuando todas las propiedades ya están mapeadas.
        ApplyUtcDateTimeConverters(modelBuilder);
    }

    /// <summary>
    /// Configura las entidades del módulo de autenticación de BrahmCQRS.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    private static void ConfigureAuth(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthRole>(entity =>
        {
            entity.ToTable("AuthRoles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(250);

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.ToTable("AuthUsers");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuthSession>(entity =>
        {
            entity.ToTable("AuthSessions");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RevokedToken>(entity =>
        {
            entity.ToTable("RevokedTokens");
            entity.HasKey(e => e.Id);

            // 850 caracteres es el máximo indexable en SQL Server (1700 bytes).
            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(850);

            entity.Property(e => e.Reason)
                .HasMaxLength(250);

            entity.HasIndex(e => e.Token);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }

    /// <summary>
    /// Configura la entidad de paquetes.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    private static void ConfigurePackages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Package>(entity =>
        {
            entity.ToTable("Packages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Includes)
                .HasMaxLength(2000);

            entity.Property(e => e.Duration)
                .HasMaxLength(100);

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.HasIndex(e => e.DisplayOrder);
        });
    }

    /// <summary>
    /// Configura la entidad de citas.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    private static void ConfigureAppointments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.AppointmentDate)
                .IsRequired();

            entity.Property(e => e.Location)
                .HasMaxLength(250);

            entity.Property(e => e.Notes)
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.AdminNotes)
                .HasMaxLength(1000);

            // Restrict: un paquete con citas no se puede borrar.
            entity.HasOne(e => e.Package)
                .WithMany(p => p.Appointments)
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PackageId);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.AppointmentDate);
            entity.HasIndex(e => e.Status);
        });
    }

    /// <summary>
    /// Configura las entidades de la galería.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    private static void ConfigureGallery(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GalleryCategory>(entity =>
        {
            entity.ToTable("GalleryCategories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.Slug)
                .IsUnique();

            entity.HasIndex(e => e.DisplayOrder);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("Photos");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .HasMaxLength(200);

            entity.Property(e => e.AltText)
                .HasMaxLength(300);

            entity.Property(e => e.ThumbPath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.MediumPath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.LargePath)
                .IsRequired()
                .HasMaxLength(500);

            // Restrict: el servicio borra primero las fotos y sus archivos en disco.
            entity.HasOne(e => e.GalleryCategory)
                .WithMany(c => c.Photos)
                .HasForeignKey(e => e.GalleryCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.GalleryCategoryId, e.DisplayOrder });
            entity.HasIndex(e => e.IsFeatured);
        });
    }

    /// <summary>
    /// Configura la entidad de mensajes de contacto.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    private static void ConfigureContactMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.ToTable("ContactMessages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasMaxLength(20);

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
        });
    }

    /// <summary>
    /// Aplica el conversor UTC a todas las propiedades de fecha del modelo.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    /// <remarks>
    /// SQL Server no persiste el <see cref="DateTimeKind"/>. Sin este conversor,
    /// las fechas leídas regresan como Unspecified y las conversiones de zona
    /// horaria de <c>ITimeProvider</c> lanzan excepción.
    /// </remarks>
    private static void ApplyUtcDateTimeConverters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(NullableUtcConverter);
                }
            }
        }
    }

    /// <summary>
    /// Siembra el rol de administrador y el usuario administrador inicial.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    private static void SeedAuthData(ModelBuilder modelBuilder)
    {
        // Valor fijo: EF exige datos deterministas en HasData.
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<AuthRole>().HasData(
            new AuthRole
            {
                Id = 1,
                Name = "Admin",
                Description = "Administrador con acceso total al sistema",
                Activated = true,
                CreatedDate = seedDate
            },
            new AuthRole
            {
                Id = 2,
                Name = "User",
                Description = "Usuario con acceso limitado",
                Activated = true,
                CreatedDate = seedDate
            });

        // Hash BCrypt de "Admin123!". Cambiar antes de producción.
        const string InitialPasswordHash = "$2a$12$1djuNPTG9ai6nB4FX3F6megWmLCGeSd1kKBI8qToJ3X8Yg6x5G7F6";

        modelBuilder.Entity<AuthUser>().HasData(
            new AuthUser
            {
                Id = 1,
                Name = "Abraham",
                LastName = "Cruz",
                Email = "braham.gc@gmail.com",
                PasswordHash = InitialPasswordHash,
                RoleId = 1,
                EmailVerified = true,
                HasPassword = true,
                Activated = true,
                CreatedDate = seedDate
            });
    }
}
