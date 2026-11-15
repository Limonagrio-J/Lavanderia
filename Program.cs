using PrototipoCRUD.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ClienteDAO>();

var app = builder.Build();

// Crea la tabla "clientes" (y las columnas nuevas) si aún no existen.
// Si la base de datos no responde, la aplicación igual abre y muestra un aviso amable.
using (var scope = app.Services.CreateScope())
{
    try
    {
        scope.ServiceProvider.GetRequiredService<ClienteDAO>().AsegurarEstructura();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "No se pudo preparar la base de datos. Revisa que PostgreSQL esté encendido y la cadena de conexión 'Pepokita' en appsettings.json.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
