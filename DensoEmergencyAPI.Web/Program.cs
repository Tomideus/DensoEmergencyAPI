using DensoEmergencyAPI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllers(); // ✅ Agregar esto para los Controllers
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Registrar el servicio de base de datos
builder.Services.AddSingleton<DatabaseService>();

// Registrar el BackgroundService para ejecución automática
builder.Services.AddHostedService<AsignacionBackgroundService>(); // ✅ Agregar esto

var app = builder.Build();

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers(); // ✅ Agregar esto para mapear los endpoints de la API
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();