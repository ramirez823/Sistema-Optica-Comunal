using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using OC.Core.Contracts.IRepositories;
using OC.Core.Domain.Entities;
using OC.Data.Context;
using OC.Data.Repositories;
using OC.Web.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


// Forzar cultura invariante para toda la aplicaci?n
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// ... resto de servicios

// --- 1. REGISTRO DE SERVICIOS (Antes de builder.Build) ---

// Configuraci?n de Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("OC.Data")));

// Registro de Repositorios
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// CONFIGURACI?N DE AUTENTICACI?N (Se movi? aqu? arriba)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        // Forzar model binding con cultura invariante
        options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"El valor '{x}' no es v?lido.");
    });

//Decimales de CR
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IGenericRepository<DetalleVenta>, GenericRepository<DetalleVenta>>();
builder.Services.AddScoped<IGenericRepository<Usuario>, GenericRepository<Usuario>>();

//SLA
// Agregar al final de la configuraci?n de servicios
builder.Services.AddHostedService<SLAMonitorService>();
builder.Services.AddHostedService<TicketAutoCloseService>();

//Valor Clinico
builder.Services.AddScoped<IGenericRepository<ValorClinico>, GenericRepository<ValorClinico>>();
builder.Services.AddScoped<IValorClinicoRepository, ValorClinicoRepository>();

//Detalle Pedido
builder.Services.AddScoped<IGenericRepository<DetallePedido>, GenericRepository<DetallePedido>>();
//Documento
builder.Services.AddScoped<IGenericRepository<DocumentoExpediente>, GenericRepository<DocumentoExpediente>>();

//TecnologiaLente y Aro
builder.Services.AddScoped<IGenericRepository<TecnologiaLente>, GenericRepository<TecnologiaLente>>();
builder.Services.AddScoped<IGenericRepository<Aro>, GenericRepository<Aro>>();

//Proveedor — repositorio dedicado
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();

//Peso de archivo
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 10 * 1024 * 1024; // 10 MB
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// CIT-RF-016 Recordatorios de citas
builder.Services.Configure<RecordatorioCitasOptions>(
    builder.Configuration.GetSection(RecordatorioCitasOptions.SectionName));
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<RecordatorioCitasBackgroundService>();
builder.Services.AddScoped<ITotpService, TotpService>();

// --- LA L?NEA FRONTERIZA (Solo una vez) ---
var app = builder.Build();

// --- 2. SEEDING DE DATOS (Despu?s de Build, antes de los Middlewares) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        try
        {
            // Si falla Migrate (historial inconsistente), igual debemos crear tablas requeridas.
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error aplicando migraciones (continuando con asegura-tablas).");
        }

        OC.Data.Context.DbInitializer.EnsureOrdenesTrabajoTable(context);
        OC.Data.Context.DbInitializer.EnsureEnviosNotificacionTable(context);
        OC.Data.Context.DbInitializer.EnsureCitasNotificationColumns(context);
        OC.Data.Context.DbInitializer.EnsurePacienteLockoutColumns(context);
        OC.Data.Context.DbInitializer.EnsureUsuarioSeguridadColumns(context);
        OC.Data.Context.DbInitializer.EnsurePermisoRutaDocumentoIncapacidadColumn(context);
        OC.Data.Context.DbInitializer.EnsureProductoRutaImagenColumn(context);
        OC.Data.Context.DbInitializer.EnsureValorClinicoAddColumns(context);
        OC.Data.Context.DbInitializer.EnsureOrdenTrabajoNewColumns(context);
        OC.Data.Context.DbInitializer.EnsureProveedorSchema(context);
        OC.Data.Context.DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurri? un error al sembrar la base de datos.");
    }
}

// --- 3. CONFIGURACI?N DEL PIPELINE (Middlewares) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // 1. Primero Routing
app.UseSession();

app.UseAuthentication(); // 2. Luego Qui?n eres
app.UseAuthorization();  // 3. Finalmente Qu? puedes hacer



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();