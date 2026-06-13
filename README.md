# Sistema Óptica Comunal

Sistema administrativo y clínico para una red de ópticas, en **.NET 8** con ASP.NET Core MVC y Clean Architecture.

## Stack

- **.NET 8** + ASP.NET Core MVC
- **Entity Framework Core** con SQL Server (Code First + Migraciones)
- **Razor Views** para el panel interno y vistas de paciente
- **API REST** integrada para endpoints consumibles (órdenes de trabajo)
- **Bootstrap 5** para la UI; **jQuery Validation** para formularios
- **ASP.NET Identity** para autenticación
- **Roles personalizados** con permisos granulares por módulo

## Arquitectura

```
OC.Core         ← Entidades de dominio, enums, contratos de repositorios
OC.Data         ← EF Core, DbContext, migraciones, configuraciones, repositorios
OC.Web          ← MVC (Controllers + Views), API REST embebida, Program.cs
```

La dirección de dependencias es: `OC.Web → OC.Core`, `OC.Data → OC.Core`, `OC.Web → OC.Data`. OC.Core es la única capa que no conoce ni a EF Core ni a ASP.NET — todo lo que tenga que ver con la web o la persistencia vive en las otras dos capas.

OC.Web es **MVC tradicional con vistas Razor** (no una API pura). Hay áreas funcionales diferenciadas: panel administrativo, portal de pacientes, y una API REST interna para integraciones externas (actualmente: órdenes de trabajo).

## Módulos funcionales

El sistema cubre la operación completa de una óptica con varias sucursales:

- **Pacientes y expedientes clínicos** — registro, historial, valores clínicos por consulta, documentos adjuntos
- **Citas** — solicitud pública (landing), gestión interna, recordatorios automáticos
- **Órdenes de trabajo** — creación, seguimiento, taller interno; expone API REST para integraciones
- **Inventario y productos** — aros, lentes, tecnologías; con categorías y proveedores
- **Compras** — pedidos a proveedores con detalle
- **Ventas** — punto de venta, métodos de pago, detalle por producto
- **Sucursales y empleados** — gestión multi-sucursal, planillas, asistencia, permisos
- **Tickets de soporte** — comentarios por ticket
- **Reportes** — módulo de reportería
- **Landing pública** — información, contacto, solicitud de cita

## Decisiones de arquitectura

### Clean Architecture con tres proyectos

Domain, Infrastructure y Presentation están separados en tres proyectos. OC.Core no conoce ni a EF Core ni a ASP.NET — todo lo que tenga que ver con la web o la persistencia vive en las otras dos capas. Los servicios de aplicación (como `SLAService`) viven dentro de OC.Core, junto a las entidades, porque su ciclo de vida no se distingue del de las entidades mismas y separarlos agregaría una capa sin reglas de dependencia que se estén rompiendo en la práctica.

### Code First con migraciones

El proyecto tiene migraciones de EF Core versionadas en `OC.Data/Migrations/`. La inicial se aplica con `Update-Database` desde la consola de NuGet o `dotnet ef database update` desde la CLI. Agregar una nueva migración es `dotnet ef migrations add <Nombre> --project OC.Data --startup-project OC.Web`.

### Repositorios específicos sólo donde hace falta

Hay un `GenericRepository<T>` para los CRUDs estándar, y repositorios específicos (`ProveedorRepository`, `ValorClinicoRepository`) sólo cuando la lógica de consultas no encaja en un genérico. La abstracción sigue a la necesidad: el genérico cubre el 90% de los casos, lo específico aparece cuando la query es demasiado particular para un `FindAsync(predicate)`.

### Identidad con permisos granulares

Los roles no son un enum plano — hay una entidad `Permiso` ligada a roles con módulos del sistema. El `PermisoController` configura qué puede hacer cada rol por sección. El sistema se entrega con roles preconfigurados para Admin, Optometrista, Recepción, Técnico y Paciente.

### API REST + MVC en el mismo proyecto

`OrdenesTrabajoApiController` convive con los controllers MVC tradicionales. El routing los separa: las acciones API usan `[Route("api/ordenes-trabajo")]` y devuelven JSON; los controllers MVC devuelven vistas. Esto evita un deploy separado para una sola API chica.

## Cómo correr el proyecto

**Prerrequisitos:**
- .NET 8 SDK
- SQL Server (Express, LocalDB o Developer)

```bash
# Restaurar y compilar
dotnet restore
dotnet build

# Aplicar migraciones
dotnet ef database update --project OC.Data --startup-project OC.Web

# Levantar
dotnet run --project OC.Web
```

La app queda en `http://localhost:5160` por defecto. La landing pública está en `/`, el panel interno en `/Home` (requiere login), y el portal de pacientes en `/PacienteDashboard`.

**Connection string:** vive en `OC.Web/appsettings.json` bajo `DefaultConnection`. Está pensado para SQL Server local (`Server=localhost` o `Server=.\SQLEXPRESS`). Hay también un `appsettings.Development.json` con la cadena para desarrollo. Ambos archivos están excluidos del repositorio por `.gitignore` — cada desarrollador mantiene el suyo.

## Credenciales iniciales

Al ejecutar por primera vez, el seeder crea el usuario administrador. Los valores exactos están en `OC.Web/Services/Seeding/...` (revisar el código del seeder para los valores actuales). Cambiar inmediatamente en cualquier deploy real.

## Lo que falta

- **Tests automatizados.** No hay proyecto `tests/`. La estructura lo permite: OC.Core es unit-testeable, OC.Data se puede probar con EF Core InMemory, y OC.Web con `WebApplicationFactory`.
- **Refactor de nombres.** Hay un controller `Expedientess` con doble ese — es un bug del scaffold original que arrastra el proyecto desde el inicio.
- **Versionado de la API.** La API REST de órdenes de trabajo no tiene versionado en URL. Para integraciones reales: `/api/v1/ordenes-trabajo`.
- **Docker / CI.** No hay Dockerfile ni pipeline. Para deploy: Dockerfile multi-stage + GitHub Actions.
- **Auditoría.** Las entidades críticas (órdenes, ventas, cambios de permiso) no tienen tabla de auditoría. Para un sistema clínico real es indispensable.

## Estructura del proyecto

```
Sistema Optica Comunal/
├── SistemaOpticaComunal.sln
├── README.md
├── home/                              ← notas de desarrollo (no productivo)
├── OC.Core/
│   ├── Common/                        ← PagedResult<T> y utilidades compartidas
│   ├── Contracts/IRepositories/       ← IGenericRepository + repos específicos
│   ├── Domain/
│   │   ├── Entities/                  ← 25 entidades (Aro, Cita, OrdenTrabajo, ...)
│   │   └── Enums/
│   └── Services/                      ← SLAService y futuros servicios de dominio
├── OC.Data/
│   ├── Configurations/                ← Configuración Fluent API por entidad
│   ├── Context/                       ← AppDbContext
│   ├── Migrations/
│   └── Repositories/                  ← GenericRepository + específicos
└── OC.Web/
    ├── Controllers/                   ← 27 controllers (MVC + API)
    ├── Helpers/
    ├── Models/
    ├── Services/                      ← servicios de aplicación / infraestructura web
    ├── ViewComponents/
    ├── ViewModels/
    ├── Views/                         ← 123 vistas Razor
    ├── wwwroot/                       ← assets estáticos, uploads
    ├── Program.cs
    ├── appsettings.json               ← (gitignored)
    └── appsettings.Development.json   ← (gitignored)
```
