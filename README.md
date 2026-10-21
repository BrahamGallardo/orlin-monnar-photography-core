# Orlin Monnar Photography — Core

API REST del sitio de fotografía: gestor de citas, galería, paquetes y formulario de contacto.

Arquitectura limpia con CQRS sobre la librería **BrahmCQRS**. Consumida por dos clientes independientes: la landing pública (`/`) y el panel de administración (`/admin`).

---

## Stack

| Componente | Versión |
|---|---|
| .NET / C# | 8.0 / 12 |
| Entity Framework Core | 8.0.11 (SQL Server) |
| AutoMapper | 12.0.1 |
| Swashbuckle | 6.6.2 |
| MailKit | 4.14.1 |
| BrahmCQRS | ensamblados en `libs/` |

> **Restricción:** no se usa sintaxis, API ni paquete posterior a lo declarado en los `.csproj`. AutoMapper se queda en la línea 12.x a propósito: la 13 cambia el registro por DI y la 14 dejó de ser gratuita.

---

## Estructura

```
Orlin Monnar Photography Core/
├── orlin-monnar-photography.sln
├── libs/                       Ensamblados de BrahmCQRS (versionados)
├── omp-domain/                 Entidades y specifications
│   ├── Common/                 Constantes de estatus
│   ├── Entities/               Package, Appointment, GalleryCategory, Photo, ContactMessage
│   └── Specifications/
├── omp-application/            Lógica de negocio
│   ├── Contracts/Services/     Interfaces de servicios
│   ├── DTOs/                   Objetos de transferencia
│   ├── Mappings/               Perfil de AutoMapper y extensiones
│   └── Services/               Implementaciones
├── omp-infrastructure/         Persistencia y servicios técnicos
│   ├── Configuration/          Settings tipados
│   ├── Migrations/
│   ├── Persistence/            OmpDbContext
│   └── Services/               CaptchaValidator
└── omp-api/                    Controllers, Program.cs, appsettings
```

Dependencias: `api → domain + application + infrastructure`, `infrastructure → domain + application`, `application → domain`.

---

## Dependencia de BrahmCQRS

La librería vive fuera de esta solución, en `D:\Projects\Personal\BrahmCQRS`. Se consume por **ensamblado**, no por referencia de proyecto: los cuatro DLL están en `libs/` y versionados en git (hay una excepción explícita a la regla `*.dll` en el `.gitignore`).

Se decidió así porque `ProjectReference` a un `.csproj` fuera del `.sln` rompe el restore de Visual Studio con **NU1105**.

**Al actualizar BrahmCQRS**, recompílala en Release y recopia los cuatro archivos:

```
BrahmCQRS.Domain.dll
BrahmCQRS.Application.dll
BrahmCQRS.Infrastructure.dll
BrahmCQRS.Shared.dll
```

desde `src\<Proyecto>\bin\Release\net8.0\` hacia `libs\`.

> Con referencias por ensamblado, NuGet **no** arrastra las dependencias transitivas de la librería. Todo lo que necesita `BrahmCQRS.Infrastructure` (MailKit, BCrypt, JwtBearer, EF Core, IdentityModel, Linq.Dynamic.Core) está declarado explícitamente en `omp-infrastructure.csproj`. Si actualizas la librería y agrega un paquete nuevo, hay que declararlo aquí también.

Lo que BrahmCQRS resuelve y **no se reescribe**: repositorios Command/Query, UnitOfWork, `BaseSpecification`, `PaginatedList`, módulo Auth completo (JWT, sesiones, revocación, hashing), `EmailService` SMTP, `CurrentUserService` e `ITimeProvider`.

---

## Requisitos

- SDK de .NET 8
- SQL Server accesible
- `dotnet-ef` 8.0.11 (declarado en `omp-api/.config/dotnet-tools.json`)

```powershell
dotnet tool restore
```

---

## Configuración

`appsettings.json` trae la plantilla con valores vacíos; `appsettings.Development.json` trae los de desarrollo.

| Sección | Qué configura |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `BrahmCQRS:Auth:JWT` | Secreto, issuer, audience, expiraciones, timeouts por rol |
| `Mail` | SMTP. `EnableTestMode: true` evita envíos reales |
| `Rutas` | Rutas de recursos embebidos en correos |
| `Display:TimeZone` | Zona horaria de presentación. Único lugar donde se sale de UTC |
| `ContactUs` / `Booking` | Destinatarios de los avisos internos |
| `ReCaptcha` | Clave secreta, score mínimo. `EnableValidation: false` solo en dev |
| `Storage` | Raíz en disco, URL pública, tamaño máximo, extensiones permitidas |
| `Cors:AllowedOrigins` | Orígenes del dev server. En producción CORS está apagado |
| `ForwardedHeaders:KnownProxies` | IPs de nginx si no corre en el mismo servidor. Vacío = solo loopback |

**Secretos.** Ningún archivo versionado lleva credenciales reales. `appsettings.json` y `appsettings.Development.json` traen las claves sensibles vacías o con valores ficticios; `appsettings.Production.json` está en el `.gitignore`.

### Desarrollo: user secrets

`omp-api.csproj` declara `UserSecretsId`, así que la cadena de conexión vive fuera del repositorio (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`) y se carga solo con `ASPNETCORE_ENVIRONMENT=Development`, por encima de `appsettings.Development.json`.

```powershell
# desde la carpeta de la solución
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=<servidor>;Database=<base>;User Id=<usuario>;Password=<contraseña>;Encrypt=False" --project omp-api
dotnet user-secrets list --project omp-api
```

Sin este secreto la API arranca, pero la primera petición que toque la base falla. Si `dotnet ef` no toma el secreto, pásale el ambiente: `-- --environment Development`.

### Producción: variables de entorno

En producción estas claves deben venir de variables de entorno del servidor. Tienen prioridad sobre `appsettings.Production.json`; el separador de sección es `__` (doble guion bajo).

| Variable de entorno | Notas |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena de SQL Server |
| `BrahmCQRS__Auth__JWT__SecretKey` | **Mínimo 32 caracteres** o la aplicación no arranca |
| `Mail__SmtpServer` / `Mail__SmtpPort` | Servidor SMTP |
| `Mail__SenderEmail` / `Mail__SenderPassword` | Cuenta remitente (contraseña de aplicación) |
| `ReCaptcha__SecretKey` | Clave secreta real de reCAPTCHA v3 |
| `Storage__RootPath` | Ruta absoluta en disco de las imágenes |
| `Storage__PublicBaseUrl` | URL pública de los derivados (por defecto `/media`) |
| `ForwardedHeaders__KnownProxies__0` | Solo si nginx corre en otro host o contenedor (`__1`, `__2`… para más) |

### Producción: nginx

Un solo dominio (`https://www.orlinmonnarphotography.com`; el dominio sin `www` y HTTP redirigen ahí) con cuatro rutas. Por eso CORS queda apagado fuera de desarrollo.

| Ruta | Quién la sirve | Caché |
|---|---|---|
| `/` | nginx, carpeta de la landing (sin build) | HTML, CSS y JS `no-cache` (revalidan con ETag: no llevan hash); imágenes y fuentes 7 días |
| `/admin/` | nginx, `dist/` del panel; `/admin/*` cae en `/admin/index.html` | `index.html` `no-cache`; `/admin/assets/` inmutable un año |
| `/api/` | Kestrel (`127.0.0.1:5000`) | La decide la API |
| `/media/` | Kestrel (`UseStaticFiles` sobre `Storage:RootPath`) | `immutable` un año, lo pone la API |

Archivos en [`deploy/`](deploy/):

| Archivo | Destino en el servidor |
|---|---|
| `deploy/nginx/orlin-monnar-photography.conf` | `/etc/nginx/sites-available/` (enlazado en `sites-enabled/`) |
| `deploy/nginx/snippets/*.conf` | `/etc/nginx/snippets/` |
| `deploy/systemd/omp-api.service` | `/etc/systemd/system/` |
| `deploy/systemd/omp-api.env.example` | `/etc/omp/omp-api.env` (completar; `root:omp`, `chmod 640`) |

Reglas que no se deducen leyendo la configuración:

- **`^~` en `/api/`, `/media/` y `/admin/`.** Las regex de la landing ganan sobre un prefijo normal; sin `^~`, `/admin/index.html` y `/admin/assets/*.js` se buscarían en la carpeta de la landing.
- **El límite de subida es el mismo valor en tres lugares:** `Storage:MaxUploadSizeMB` (de ahí salen `FormOptions` y Kestrel), `client_max_body_size` de `location = /api/admin/gallery/photos` y `storage.maxUploadSizeMb` del panel. Si nginx dejara pasar más que Kestrel, Kestrel cortaría la conexión y el navegador recibiría **502**, no 413.
- **`proxy_read_timeout 180s` en la subida.** El panel espera hasta 120 s; con los 60 s por defecto de nginx una foto pesada daría 504 mientras ImageSharp sigue procesando.
- **`X-Forwarded-For $remote_addr`.** nginx es el borde, así que el valor que mande el cliente se sustituye. Sin estos encabezados el rate limiting por IP trata a todos los visitantes como uno solo. Si algún día se pone un CDN delante, hay que revisar esto y `ForwardedHeaders:KnownProxies`.
- **Brotli** está comentado en `omp-compression.conf`: requiere `libnginx-mod-http-brotli-filter`. WebP nunca se comprime.

### Producción: checklist de despliegue

En la máquina de desarrollo:

```powershell
# API (framework-dependent: el servidor necesita aspnetcore-runtime-8.0)
dotnet publish omp-api -c Release -o .\publish
dotnet ef migrations script --idempotent --project omp-infrastructure --startup-project omp-api -o deploy.sql

# Panel
cd "..\Orlin Monnar Photography Admin"
npm ci
npm run build
```

En el servidor:

1. Instalar `nginx`, `aspnetcore-runtime-8.0` y `certbot`. Crear el usuario `omp` y las carpetas `/var/www/omp/{api,landing,admin}`, `/var/omp/media` (dueño `omp`) y `/var/www/certbot`.
2. Copiar `publish\*` a `/var/www/omp/api/`.
3. Aplicar `deploy.sql` en la base de producción y cambiar la contraseña del administrador sembrado.
4. Copiar la landing a `/var/www/omp/landing/` **sin** `.git`, `.gitignore`, `scss/`, `README.md`, `readme.txt`, `node_modules/` ni `js/app/config.example.js`, y crear ahí `js/app/config.js` con `baseUrl: ''` y la clave real de reCAPTCHA.
5. Copiar el **contenido** de `dist/` del panel a `/var/www/omp/admin/` (debe quedar `/var/www/omp/admin/index.html`).
6. Crear `/etc/omp/omp-api.env` y `systemctl daemon-reload && systemctl enable --now omp-api`.
7. Copiar sitio y snippets, quitar `sites-enabled/default` (choca con `default_server`), y:
   ```bash
   certbot certonly --webroot -w /var/www/certbot -d www.orlinmonnarphotography.com -d orlinmonnarphotography.com
   nginx -t && systemctl reload nginx
   ```
   El primer `-d` da nombre a la carpeta `/etc/letsencrypt/live/www.orlinmonnarphotography.com/`. Antes del primer certificado, el sitio no pasa `nginx -t` porque los `ssl_certificate` no existen todavía: habilita temporalmente solo el bloque del puerto 80 para emitirlo.

Verificación:

```bash
D=https://www.orlinmonnarphotography.com
curl -sI $D/                          | grep -iE '^HTTP|cache-control'                 # 200, no-cache
curl -sI $D/css/style.css -H 'Accept-Encoding: gzip' | grep -iE 'cache-control|content-encoding'
curl -sI $D/admin                     | grep -iE '^HTTP|location'                      # 301 → /admin/
curl -sI $D/admin/appointments        | grep -iE '^HTTP|content-type'                  # 200 text/html
curl -sI $D/admin/assets/<archivo>.js | grep -i cache-control                          # immutable
curl -s  $D/api/packages -o /dev/null -w '%{http_code} %{content_type}\n'              # 200 application/json
curl -sI $D/media/<ruta>.webp         | grep -iE 'cache-control|content-encoding'      # immutable, sin content-encoding
curl -sI https://orlinmonnarphotography.com/ | grep -i location                         # → https://www...
```

Desde el panel: una foto mayor a `Storage:MaxUploadSizeMB` debe mostrar el mensaje de 413, y el sexto login fallido en un minuto debe dar 429 solo para tu IP (prueba de que llega la IP real).

---

## Base de datos

```powershell
# desde la carpeta de la solución
dotnet ef migrations add <Nombre> --project omp-infrastructure --startup-project omp-api
dotnet ef database update --project omp-infrastructure --startup-project omp-api
```

Si no puedes tocar la base directamente (producción), genera el script:

```powershell
dotnet ef migrations script --idempotent --project omp-infrastructure --startup-project omp-api -o deploy.sql
```

> No uses `dotnet ef dbcontext script`: crea el esquema sin registrar nada en `__EFMigrationsHistory` y deja a EF desincronizado.

**Seed.** La migración inicial siembra los roles `Admin` y `User`, y un usuario administrador:

```
braham.gc@gmail.com / Admin123!
```

Credencial de arranque, **cambiar antes de producción**. El login rechaza usuarios sin `EmailVerified`, por eso el seed lo trae en `true`.

---

## Ejecución

```powershell
dotnet run --project omp-api
```

| | URL |
|---|---|
| HTTP | `http://localhost:5081` |
| HTTPS | `https://localhost:7132` |
| Swagger | `/swagger` (solo en Development) |

Swagger tiene el esquema Bearer configurado: pega el token JWT sin el prefijo `Bearer`.

---

## Convenciones

Estas reglas salieron de auditar BrahmCQRS y no son opcionales.

**Fechas en UTC.** Todo se persiste en UTC. `AddBrahmCQRSCore` se registra con `TimeZoneInfo.Utc` para que la auditoría también lo sea. `OmpDbContext` aplica un `ValueConverter` global que restablece `Kind = Utc` al leer, porque `datetime2` no lo conserva y `ITimeProvider.ConvertToServerTime` lanza excepción si el `Kind` no es UTC. La conversión a hora local ocurre en dos lugares y solo dos: `EmailTemplateService` y el navegador.

**Actualizaciones.** Nunca mapear un DTO a una entidad nueva y llamar `UpdateAsync`: el repositorio marca la entidad completa como `Modified` y escribiría `CreatedDate` en `0001-01-01`. Siempre leer la entidad, mapear el DTO encima y actualizar esa instancia. `MappingProfile` lo respalda con `IgnoreIdentityAndAudit()`, que ignora `Id` y los cuatro campos de auditoría en toda dirección DTO→entidad. **No hay un solo `ReverseMap()`** en el perfil, a propósito.

**Correos.** Los envíos van en `try/catch` dentro del servicio. Si el SMTP falla, la cita o el mensaje **se guardan igual** y el fallo se registra en el log.

**Endpoints públicos.** Booking y contacto son anónimos, con reCAPTCHA validado en el servicio y rate limiting (`RateLimitPolicies.PublicForms`, 5 req/min por IP). Los DTO de entrada están separados de los de salida: `CreateAppointmentRequestDto` no permite fijar `Status`.

**Autenticación.** En refresh, el `userId` sale **siempre** del claim del token, nunca del body.

**Borrado.** BrahmCQRS no expone borrado físico, solo `SoftDeleteAsync`. `DeletePhotoAsync` desactiva el registro y borra los tres WebP del disco: es irreversible y el panel no ofrece reactivar fotos.

---

## Imágenes

Al subir una foto se generan tres derivados WebP y **el original se descarta**:

| Versión | Lado mayor | Uso |
|---|---|---|
| thumb | ~500 px | Grids y carruseles |
| medium | ~1200 px | Galería |
| large | ~2560 px | Lightbox |

Conversión a sRGB, strip de EXIF. Los binarios van a disco vía `IStorageService`; en base de datos solo quedan las rutas públicas y los metadatos. `Photo.Width` y `Photo.Height` existen para emitir `aspect-ratio` en el grid y evitar el salto de layout.

---

## Estado

| Tarea | Estado |
|---|---|
| B1 — Setup de solución | Completo |
| B2 — Configuración base | Completo |
| B3 — Modelo de dominio | Completo |
| B4 — Persistencia | Completo |
| B5 — Servicios de aplicación | Completo |
| B6 — Controllers | Pendiente |
| B7 — Upload y procesamiento de imágenes | Pendiente |
| B8 — Emails transaccionales | Plantillas base listas, falta branding y pruebas de entregabilidad |

> `IStorageService` e `IImageProcessingService` tienen contrato pero no implementación. La API arranca, pero cualquier endpoint de galería fallará al resolver DI hasta que B7 esté listo.

Referencia completa: `Plan de Arquitectura y Roadmap Técnico - Orlin Monnar Photography.docx` (v1.2).
