using AutoMapper;
using omp_application.DTOs.Appointment;
using omp_application.DTOs.ContactUs;
using omp_application.DTOs.Gallery;
using omp_application.DTOs.Package;
using omp_domain.Entities;

namespace omp_application.Mappings;

/// <summary>
/// Perfil de mapeos entre entidades de dominio y DTOs.
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Registra todos los mapeos de la aplicación.
    /// </summary>
    public MappingProfile()
    {
        CreateMap<Package, PackageDto>();
        CreateMap<PackageDto, Package>()
            .ForMember(destination => destination.Appointments, options => options.Ignore())
            .IgnoreIdentityAndAudit();

        CreateMap<Appointment, AppointmentDto>()
            .ForMember(
                destination => destination.PackageName,
                options => options.MapFrom(source => source.Package != null ? source.Package.Name : null));

        // Solo del formulario público hacia la entidad: el estatus y las fechas de
        // confirmación o cancelación los fija el servicio, nunca el cliente.
        CreateMap<CreateAppointmentRequestDto, Appointment>()
            .ForMember(destination => destination.Status, options => options.Ignore())
            .ForMember(destination => destination.ConfirmedDate, options => options.Ignore())
            .ForMember(destination => destination.CancelledDate, options => options.Ignore())
            .ForMember(destination => destination.AdminNotes, options => options.Ignore())
            .ForMember(destination => destination.Package, options => options.Ignore())
            .IgnoreIdentityAndAudit();

        // CoverPhoto y PhotoCount los calcula GalleryService a partir de las fotografías
        // activas: no hay origen en la entidad. El Ignore() es defensivo — hoy AutoMapper
        // deja los miembros sin resolver en su valor por defecto porque Program.cs no llama
        // AssertConfigurationIsValid, y ninguno de los dos nombres se aplana por convención.
        CreateMap<GalleryCategory, GalleryCategoryDto>()
            .ForMember(destination => destination.CoverPhoto, options => options.Ignore())
            .ForMember(destination => destination.PhotoCount, options => options.Ignore());

        // Sin cambios: GalleryCategory no tiene CoverPhoto ni PhotoCount, así que no se
        // pueden ignorar en esta dirección (ForMember exige un miembro del destino).
        // Tampoco hace falta: AutoMapper valida con MemberList.Destination y nunca se queja
        // de miembros de origen sin usar.
        CreateMap<GalleryCategoryDto, GalleryCategory>()
            .ForMember(destination => destination.Photos, options => options.Ignore())
            .IgnoreIdentityAndAudit();

        CreateMap<Photo, PhotoDto>()
            .ForMember(destination => destination.ThumbUrl, options => options.MapFrom(source => source.ThumbPath))
            .ForMember(destination => destination.MediumUrl, options => options.MapFrom(source => source.MediumPath))
            .ForMember(destination => destination.LargeUrl, options => options.MapFrom(source => source.LargePath));

        // Las rutas y dimensiones las produce el procesamiento de imágenes,
        // no el panel: no se mapean de regreso.
        CreateMap<PhotoDto, Photo>()
            .ForMember(destination => destination.ThumbPath, options => options.Ignore())
            .ForMember(destination => destination.MediumPath, options => options.Ignore())
            .ForMember(destination => destination.LargePath, options => options.Ignore())
            .ForMember(destination => destination.Width, options => options.Ignore())
            .ForMember(destination => destination.Height, options => options.Ignore())
            .ForMember(destination => destination.FileSizeBytes, options => options.Ignore())
            .ForMember(destination => destination.GalleryCategory, options => options.Ignore())
            .IgnoreIdentityAndAudit();

        CreateMap<ContactMessage, ContactMessageDto>();

        CreateMap<CreateContactMessageRequestDto, ContactMessage>()
            .ForMember(destination => destination.Status, options => options.Ignore())
            .ForMember(destination => destination.RespondedAt, options => options.Ignore())
            .IgnoreIdentityAndAudit();
    }
}
